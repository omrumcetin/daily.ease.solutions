using ClosedXML.Excel;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JobAnalyser
{
    class Program
    {
        const string DB_CONNECTION_STRING = "Data Source=10.0.128.32:11521/MNP;User Id=MNP;Password=mnp;Validate Connection=true;";
        static void Main(string[] args)
        {
            List<LogExecution> logList = new List<LogExecution>();
            Dictionary<int, OptimizerLib> optimizerLibCache = new Dictionary<int, OptimizerLib>();
            using (OracleConnection connection = new OracleConnection(DB_CONNECTION_STRING))
            {
                connection.Open();
                string cmdQueryText = @$"
                                            SELECT  EXECUTIONGUID, 
                                                    EXECUTIONSTARTTIMESTAMP, 
                                                    EXECUTIONPLANID, 
                                                    PROGRESSCHANGETIMESTAMP, 
                                                    PROGRESS, 
                                                    OSSSERVICEJOBID, 
                                                    STATUS
                                                FROM LOG_PISON_EXECUTION_PROGRESS
                                              WHERE EXECUTIONSTARTTIMESTAMP >= TRUNC(SYSDATE)";
                using (OracleCommand command = new OracleCommand(cmdQueryText, connection))
                {
                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        reader.FetchSize = reader.RowSize * 100000;
                        while (reader.Read())
                        {
                            var logLine = new LogExecution()
                            {
                                ExecutionGuid = reader.GetGuid(0),
                                ExecutionStartTimestamp = reader.GetDateTime(1),
                                ExecutionPlanId = reader.GetInt32(2),
                                ProgressChangeTimestamp = reader.GetDateTime(3),
                                Progress = reader.IsDBNull(4) ? default(string) : reader.GetString(4),
                                OssServiceJobId = reader.IsDBNull(5) ? default(int) : reader.GetInt32(5),
                                Status = reader.IsDBNull(6) ? default(string) : reader.GetString(6)
                            };
                            logList.Add(logLine);
                        }
                    }
                }
                cmdQueryText = @$"select executionplanid, name, description from pison_execution_plan";
                using (OracleCommand command = new OracleCommand(cmdQueryText, connection))
                {
                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        reader.FetchSize = reader.RowSize * 100000;
                        while (reader.Read())
                        {
                            optimizerLibCache.Add(reader.GetInt32(0), new OptimizerLib() { OptimizerName = reader.GetString(1), JobDescription = reader.GetString(2) });
                        }
                    }
                }
            }
            var logListsByExecutionGuid = logList.GroupBy(o => o.ExecutionGuid).ToDictionary(o => o.Key, g => g.ToList());
            List<ReportLog> reportLogList = new List<ReportLog>();

            foreach (var logListByExecutionGuid in logListsByExecutionGuid)
            {
                var list = logListByExecutionGuid.Value;
                list.Sort((x1, x2) => DateTime.Compare(x1.ProgressChangeTimestamp, x2.ProgressChangeTimestamp));

                if (!optimizerLibCache.TryGetValue(list.First().ExecutionPlanId, out var job))
                {
                    continue;
                }

                var reportLog = new ReportLog()
                {
                    ExecutionGuid = list.First().ExecutionGuid,
                    ExecutionPlanId = list.First().ExecutionPlanId,
                    ExecutionStartTimestamp = list.First().ExecutionStartTimestamp,
                    OptimizerName = job.OptimizerName,
                    Description = job.JobDescription
                };
                DateTime durationStartTime = new DateTime();
                bool isNssRunning = false;
                bool isOptimizerRunning = false;
                bool isOssServiceRunning = false;
                foreach (var item in list)
                {
                    //EE part
                    if (item.Progress.StartsWith("Preparing execution"))
                    {
                        reportLog.RealExecutionStartTime = item.ProgressChangeTimestamp;
                        durationStartTime = item.ProgressChangeTimestamp;
                    }
                    //NSS part
                    else if (item.Progress.Contains("Preparing data") && !isNssRunning)
                    {
                        reportLog.ExecutionPreparingDuration = (item.ProgressChangeTimestamp - durationStartTime).TotalMinutes;
                        durationStartTime = item.ProgressChangeTimestamp;
                        isNssRunning = true;
                        isOptimizerRunning = false;
                        continue;
                    }
                    else if (item.Progress.Contains("Getting executions network snapshot") && !isNssRunning)
                    {
                        reportLog.ExecutionPreparingDuration = (item.ProgressChangeTimestamp - durationStartTime).TotalMinutes;
                        durationStartTime = item.ProgressChangeTimestamp;
                        isNssRunning = true;
                        isOptimizerRunning = false;
                        continue;
                    }
                    //Optimizer part
                    else if (item.Progress.StartsWith("Optimizer running") 
                            && !item.Progress.Contains("Getting executions network snapshot")
                            && !item.Progress.Contains("processing optimizer response")
                            && !isOptimizerRunning)
                    {
                        if (isNssRunning)
                        {
                            reportLog.NSSSnapshotPreparingDuration = (item.ProgressChangeTimestamp - durationStartTime).TotalMinutes;
                        }
                        else
                        {
                            reportLog.ExecutionPreparingDuration = (item.ProgressChangeTimestamp - durationStartTime).TotalMinutes;
                        }
                        durationStartTime = item.ProgressChangeTimestamp;
                        isNssRunning = false;
                        isOptimizerRunning = true;
                        continue;
                    }
                    else if (item.Progress.Contains("Optimizer running - processing optimizer response"))
                    {
                        reportLog.OptimizerRunningDuration = (item.ProgressChangeTimestamp - durationStartTime).TotalMinutes;
                        isOptimizerRunning = false;
                    }
                    else if (item.Progress.Contains("Sending modifications") && !isOssServiceRunning)
                    {
                        durationStartTime = item.ProgressChangeTimestamp;
                        isOssServiceRunning = true;
                    }
                    else if (item.Progress.Contains("OSS operations completed"))
                    {
                        reportLog.OssServiceImplementationDuration = (item.ProgressChangeTimestamp - durationStartTime).TotalMinutes;
                        isOssServiceRunning = false;
                        reportLog.RealExecutionEndTime = item.ProgressChangeTimestamp;
                        reportLog.OssServiceJobId = item.OssServiceJobId;
                    }
                    reportLog.RealExecutionEndTime = item.ProgressChangeTimestamp;
                }
                reportLogList.Add(reportLog);
            }
            CreateReport(reportLogList);
        }

        private static void CreateReport(List<ReportLog> reportLogList)
        {
            var workbook = new XLWorkbook();
            Dictionary<string, int> workSheetLineCache = new Dictionary<string, int>();
            reportLogList.Sort((x1, x2) => DateTime.Compare(x1.ExecutionStartTimestamp, x2.ExecutionStartTimestamp));
            AnalyseSummary(workbook, reportLogList);
            foreach (var reportLog in reportLogList)
            {
                var optimizerNormalizedName = reportLog.OptimizerName.Substring(0, Math.Min(reportLog.OptimizerName.Length, 30));
                if (!workbook.Worksheets.TryGetWorksheet(optimizerNormalizedName, out var workSheet))
                {
                    workSheet = workbook.Worksheets.Add(optimizerNormalizedName);
                    workSheetLineCache.Add(reportLog.OptimizerName, 1);
                    workSheet.Cell(1, 1).Value = "Description";
                    workSheet.Cell(1, 2).Value = "ExecutionStartTimestamp";
                    workSheet.Cell(1, 3).Value = "ExecutionPlanId";
                    workSheet.Cell(1, 4).Value = "ExecutionGuid";
                    workSheet.Cell(1, 5).Value = "Real Execution Start Time";
                    workSheet.Cell(1, 6).Value = "Real Execution End Time";
                    workSheet.Cell(1, 7).Value = "Execution Preparing Duration(Min)";
                    workSheet.Cell(1, 8).Value = "NSS Snapshot Preparing Duration(Min)";
                    workSheet.Cell(1, 9).Value = "Optimizer Running Duration(Min)";
                    workSheet.Cell(1, 10).Value = "Oss Service Running Duration(Min)";
                    workSheet.Cell(1, 11).Value = "Oss Service Job Id";
                }
                if(!workSheetLineCache.TryGetValue(reportLog.OptimizerName, out var lineNumber))
                {
                    continue;
                }
                workSheet.Cell(lineNumber + 1, 1).Value = reportLog.Description;
                workSheet.Cell(lineNumber + 1, 2).Value = reportLog.ExecutionStartTimestamp.ToString("dd/MM/yyyy HH:mm:ss");
                workSheet.Cell(lineNumber + 1, 3).Value = reportLog.ExecutionPlanId;
                workSheet.Cell(lineNumber + 1, 4).Value = reportLog.ExecutionGuid.ToString("N").ToUpper();
                workSheet.Cell(lineNumber + 1, 5).Value = reportLog.RealExecutionStartTime.AddHours(3).ToString("dd/MM/yyyy HH:mm:ss");
                workSheet.Cell(lineNumber + 1, 6).Value = reportLog.RealExecutionEndTime.AddHours(3).ToString("dd/MM/yyyy HH:mm:ss");
                workSheet.Cell(lineNumber + 1, 7).Value = reportLog.ExecutionPreparingDuration;
                workSheet.Cell(lineNumber + 1, 8).Value = reportLog.NSSSnapshotPreparingDuration;
                workSheet.Cell(lineNumber + 1, 9).Value = reportLog.OptimizerRunningDuration;
                workSheet.Cell(lineNumber + 1, 10).Value = reportLog.OssServiceImplementationDuration;
                workSheet.Cell(lineNumber + 1, 11).Value = reportLog.OssServiceJobId;
                workSheetLineCache[reportLog.OptimizerName] = lineNumber + 1;
                workSheet.Columns().AdjustToContents();
            }
            workbook.SaveAs("20210615-20210615 Beeline Optimizer Duration Report.xlsx");
        }

        private static void AnalyseSummary(XLWorkbook workbook, List<ReportLog> reportLogList)
        {
            var workSheet = workbook.Worksheets.Add("SummaryReport");
            workSheet.Cell(1, 1).Value = "Description";
            workSheet.Cell(1, 2).Value = "ExecutionStartTimestamp";
            workSheet.Cell(1, 3).Value = "ExecutionPlanId";
            workSheet.Cell(1, 4).Value = "ExecutionGuid";
            workSheet.Cell(1, 5).Value = "Real Execution Start Time";
            workSheet.Cell(1, 6).Value = "Real Execution End Time";
            workSheet.Cell(1, 7).Value = "Execution Preparing Duration(Min)";
            workSheet.Cell(1, 8).Value = "NSS Snapshot Preparing Duration(Min)";
            workSheet.Cell(1, 9).Value = "Optimizer Running Duration(Min)";
            workSheet.Cell(1, 10).Value = "Oss Service Running Duration(Min)";
            workSheet.Cell(1, 11).Value = "Oss Service Job Id";

            int lineNumber = 1;
            foreach (var reportLog in reportLogList)
            {
                if (reportLog.ExecutionPreparingDuration > 30 || reportLog.NSSSnapshotPreparingDuration > 30)
                {
                    workSheet.Cell(lineNumber + 1, 1).Value = reportLog.Description;
                    workSheet.Cell(lineNumber + 1, 2).Value = reportLog.ExecutionStartTimestamp.ToString("dd/MM/yyyy HH:mm:ss");
                    workSheet.Cell(lineNumber + 1, 3).Value = reportLog.ExecutionPlanId;
                    workSheet.Cell(lineNumber + 1, 4).Value = reportLog.ExecutionGuid.ToString("N").ToUpper();
                    workSheet.Cell(lineNumber + 1, 5).Value = reportLog.RealExecutionStartTime.AddHours(3).ToString("dd/MM/yyyy HH:mm:ss");
                    workSheet.Cell(lineNumber + 1, 6).Value = reportLog.RealExecutionEndTime.AddHours(3).ToString("dd/MM/yyyy HH:mm:ss");
                    workSheet.Cell(lineNumber + 1, 7).Value = reportLog.ExecutionPreparingDuration;
                    workSheet.Cell(lineNumber + 1, 8).Value = reportLog.NSSSnapshotPreparingDuration;
                    workSheet.Cell(lineNumber + 1, 9).Value = reportLog.OptimizerRunningDuration;
                    workSheet.Cell(lineNumber + 1, 10).Value = reportLog.OssServiceImplementationDuration;
                    workSheet.Cell(lineNumber + 1, 11).Value = reportLog.OssServiceJobId;
                    workSheet.Columns().AdjustToContents();

                    if (reportLog.ExecutionPreparingDuration > 30)
                        workSheet.Cell(lineNumber + 1, 7).Style.Font.FontColor = XLColor.Red;
                    if (reportLog.NSSSnapshotPreparingDuration > 30)
                        workSheet.Cell(lineNumber + 1, 8).Style.Font.FontColor = XLColor.Red;

                    lineNumber++;
                }
            }
        }
}

    class LogExecution
    {
        public Guid ExecutionGuid { get; set; }
        public DateTime ExecutionStartTimestamp { get; set; }
        public int ExecutionPlanId { get; set; }
        public DateTime ProgressChangeTimestamp { get; set; }
        public string Progress { get; set; }
        public int OssServiceJobId { get; set; }
        public string Status { get; set; }
    }

    class OptimizerLib
    {
        public string OptimizerName;
        public string JobDescription;
    }

    class ReportLog
    {
        public string OptimizerName { get; set; }
        public string Description { get; set; }
        public Guid ExecutionGuid { get; set; }
        public DateTime ExecutionStartTimestamp { get; set; }
        public int ExecutionPlanId { get; set; }
        public double? ExecutionPreparingDuration { get; set; }
        public double? NSSSnapshotPreparingDuration { get; set; }
        public double? OptimizerRunningDuration { get; set; }
        public double? OssServiceImplementationDuration { get; set; }
        public int? OssServiceJobId { get; set; }
        public DateTime RealExecutionStartTime { get; set; }
        public DateTime RealExecutionEndTime { get; set; }
    }
}
