using ESRollbackScheduler.Core.Db;
using Oracle.ManagedDataAccess.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace ESRollbackScheduler.Core
{
    internal static class DbProxy
    {
        public static List<OssJob> GetEnergySavingOssRollbackJobs()
        {
            Log.Debug("Getting energy saving rollback job from db");
            using (var connection = new OracleConnection(AppConfig.OracleDbConnectionString))
                using (var command = new OracleCommand(
                             @"
                            SELECT
                                OSS.JOBID,
                                OSS.SCHEDULEDSTARTDATE,
                                OSS.TARGETENDDATE,
                                OSS.ACTUALSTARTDATE,
                                OSS.ACTUALENDDATE,
                                OSS.STATE,
                                OSS.EXECUTIONGUID,
                                U.ID USERID,
                                U.NAME USERNAME,
                                OSS.JOBDESCRIPTION,
                                OSS.EXECUTIONPLANID,
                                OSS.OPERATIONTYPE
                            FROM PISON_OSSSVC_JOB OSS
                                LEFT JOIN USERV2_USERS U ON OSS.PISONUSERID = U.ID
                                LEFT JOIN PISON_EXECUTION_PLAN EP ON EP.EXECUTIONPLANID = OSS.EXECUTIONPLANID
                                LEFT JOIN PISON_OPTIMIZER PO ON PO.OPTIMIZERID = EP.OPTIMIZERID 
                            WHERE
                                    SCHEDULEDSTARTDATE >= TRUNC(SYSDATE)
                                AND ACTUALENDDATE IS NOT NULL
                                AND SERVICETYPE IN ('3G Energy Saving', '4G Energy Saving')
                                AND OPERATIONTYPE = 1",
                            connection)
                )
            {
                connection.Open();
                ReadOssJobQuery(command, out var ossJobs);
                Log.Debug($@"Retrieved jobs of following execution plan ids : {string.Join(",", ossJobs.Select(x => x.ExecutionPlanId.ToString())
                                                                                                                                     .Distinct()
                                                                                                                                     .ToArray())}");
                return ossJobs;
            }
        }

        private static void ReadOssJobQuery(OracleCommand command, out List<OssJob> ossJobs)
        {
            ossJobs = new List<OssJob>();
            using (OracleDataReader reader = command.ExecuteReader())
                while (reader.Read())
                {
                    OssJob job = ReadOssJob(reader);
                    ossJobs.Add(job);
                }
        }

        private static OssJob ReadOssJob(OracleDataReader reader)
        {
            int jobId = reader.GetInt32(0);
            DateTime scheduledStartDate = reader.GetDateTime(1);
            DateTime targetEndDate = reader.GetDateTime(2);
            DateTime actualStartDate = reader.GetDateTime(3);
            DateTime actualEndDate = reader.GetDateTime(4);
            string state = reader.GetString(5);
            Guid executionGuid = reader.GetGuid(6);
            string userId = reader.GetString(7);
            string userName = reader.GetString(8);
            string jobDescription = reader.GetString(9);
            int executionPlanId = reader.GetInt32(10);
            int operationType = reader.GetInt32(11);
            return new OssJob()
            {
                JobId = jobId,
                ScheduledStartDate = scheduledStartDate,
                TargetEndDate = targetEndDate,
                ActualStartDate = actualStartDate,
                ActualEndDate = actualEndDate,
                State = state,
                ExecutionGuid = executionGuid,
                UserId = userId,
                UserName = userName,
                JobDescription = jobDescription,
                ExecutionPlanId = executionPlanId,
                OperationType = operationType
            };
        }

        public static List<CellOriginalValue> GetCellsOriginalValues()
        {
            using (var connection = new SQLiteConnection($"Data Source={AppConfig.OriginalValuesSqlitePath}"))
            {
                using (var command = new SQLiteCommand($@"
                            SELECT PIOSSID,
                                   RADIOTECHNOLOGY,
                                   PICELLID,
                                   MONAME,
                                   PARAMETERNAME,
                                   PARAMETERREALNAME,
                                   PARAMETERVALUE,
                                   PARAMETERREALVALUE,
                                   PARAMETERNEWVALUE,
                                   JOBID,
                                   EXECUTIONPLANID,
                                   MODULENAME
                                FROM {Constants.CellOriginalValueTableName}",
                      connection))
                {
                    connection.Open();
                    //command.Parameters.AddWithValue("@executionplanid", ExecutionPlanId);
                    ReadOriginalValueQuery(command, out List<CellOriginalValue> CellsOriginalValues);
                    return CellsOriginalValues;
                }
            }
        }

        private static void ReadOriginalValueQuery(SQLiteCommand command, out List<CellOriginalValue> cellsOriginalValues)
        {
            cellsOriginalValues = new List<CellOriginalValue>();
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    CellOriginalValue cellOriginalValue = ReadOriginalValue(reader);
                    cellsOriginalValues.Add(cellOriginalValue);
                }
            }
        }

        private static CellOriginalValue ReadOriginalValue(SQLiteDataReader reader)
        {
            long piOssId = reader.GetInt32(0);
            string piClid = null;
            string radioTechnology = null;
            switch (reader.GetInt32(1))
            {
                case 1:
                    piClid = "320";
                    radioTechnology = "2G";
                    break;
                case 2:
                    piClid = "321";
                    radioTechnology = "3G";
                    break;
                case 3:
                    piClid = "322";
                    radioTechnology = "4G";
                    break;
                default:
                    break;
            }
            long piCellId = reader.GetInt64(2);
            string moname = reader.GetString(3);
            string parameterName = reader.GetString(4);
            string parameterRealName = reader.GetString(5);
            string parameterValue = reader.GetString(6);
            string parameterRealValue = reader.GetString(7);
            string parameterNewValue = !reader.IsDBNull(8) ? reader.GetString(8) : null;
            string jobId = reader.GetString(9);
            int executionPlanId = reader.GetInt32(10);
            string moduleName = !reader.IsDBNull(8) ? reader.GetString(11) : null;
            return new CellOriginalValue
            {
                PIOssId = piOssId,
                PIClid = piClid,
                RadioTechnology = radioTechnology,
                PICellId = piCellId,
                Moname = moname,
                ParameterName = parameterName,
                ParameterRealValue = parameterRealValue,
                ParameterValue = parameterValue,
                ParameterRealName = parameterRealName,
                ParameterNewValue = parameterNewValue,
                JobId = jobId,
                ExecutionPlanId = executionPlanId,
                ModuleName = moduleName
            };
        }

        public static List<RollbackConfig> GetEnergySavingJobConfig()
        {
            Log.Debug("Getting energy saving rollback job from db");
            using (var connection = new OracleConnection(AppConfig.OracleDbConnectionString))
            using (var command = new OracleCommand(
                         @"
                            SELECT EXECUTIONPLANID,
                                   DESCRIPTION,
                                   MULTISCHEDULES
                                FROM PISON_EXECUTION_PLAN
                              WHERE NAME IN ('3G Energy Saving','4G Energy Saving')
                                AND ISCLOSEDLOOP = 1
                                AND ISACTIVE = 1",
                        connection)
            )
            {
                connection.Open();
                ReadExecutionPlanQuery(command, out var rollbackConfigs);
                return rollbackConfigs;
            }
        }

        private static void ReadExecutionPlanQuery(OracleCommand command, out List<RollbackConfig> rollbackConfigs)
        {
            rollbackConfigs = new List<RollbackConfig>();
            using (OracleDataReader reader = command.ExecuteReader())
                while (reader.Read())
                {
                    RollbackConfig rollbackConfig = ReadRollbackConfig(reader);
                    rollbackConfigs.Add(rollbackConfig);
                }
        }

        private static RollbackConfig ReadRollbackConfig(OracleDataReader reader)
        {
            int executionPlanId = reader.GetInt32(0);
            XmlParser.Parse(reader.GetOracleClob(2).Value, out var rollbackScheduleAttributes);
            return new RollbackConfig
            {
                ExecutionPlanId = executionPlanId,
                EndDateLocal = rollbackScheduleAttributes.Item1,
                TimeZoneId = rollbackScheduleAttributes.Item2
            };
        }

        public static void CreateOssJob()
        {
            //get from db
            long osssrvjobid = reader.GetInt64(0);
            
            Log.Debug($"Started processing OSSSRVJOBID: {osssrvjobid}");
            
            long executionplanid = reader.GetInt64(1);
            Guid guid = new Guid(reader.GetOracleBinary(2).Value);
            DateTime dateTime = reader.GetDateTime(3);
            long optimizerid = reader.GetInt64(4);
            string jobname = null;
            if (!reader.IsDBNull(5))
            {
                jobname = reader.GetString(5);
            }
            string jobdescription = null;
            if (!reader.IsDBNull(6))
            {
                jobdescription = reader.GetString(6);
            }
            int operationType = reader.GetInt32(7);
            bool isclosedloop = operationType == 1;
            string optimizername = null;
            if (!reader.IsDBNull(8))
            {
                optimizername = reader.GetString(8);
            }
            long? userid = null;
            if (!reader.IsDBNull(9))
            {
                userid = new long?(reader.GetInt64(9));
            }
            string username = null;
            if (!reader.IsDBNull(10))
            {
                username = reader.GetString(10);
            }
            DateTimeOffset scheduledstartdate = DateTime.Now;
            DateTimeOffset targetenddate = scheduledstartdate.AddDays(1);

            string versionString = Environment.OSVersion.VersionString;
            string machineName = Environment.MachineName;
            string authinfo = $@"{Environment.UserDomainName}\{Environment.UserName}";
            string state = "Success";
            Log.Debug("Inserting commands to PISON_OSSSVC_JOB");
            string insertCommandQuery = @"INSERT /*+ APPEND PARALLEL(4)*/
                                                                           INTO pison_osssvc_job (jobid,
                                                                                                  scheduledstartdate,
                                                                                                  scheduledstartdateutc,
                                                                                                  targetenddate,
                                                                                                  targetenddateutc,
                                                                                                  state,
                                                                                                  script,
                                                                                                  scripttype,
                                                                                                  scriptcommandcount,
                                                                                                  scriptcommandcompletecount,
                                                                                                  executionguid,
                                                                                                  executionplanid,
                                                                                                  jobname,
                                                                                                  jobdescription,
                                                                                                  pisonuserid,
                                                                                                  servicetype,
                                                                                                  machineos,
                                                                                                  machinename,
                                                                                                  authinfo,
                                                                                                  operationtype)
                                                                         VALUES ( :jobid,
                                                                                 :scheduledstartdate,
                                                                                 :scheduledstartdateutc,
                                                                                 :targetenddate,
                                                                                 :targetenddateutc,
                                                                                 :state,
                                                                                 :script,
                                                                                 :scripttype,
                                                                                 :scriptcommandcount,
                                                                                 :scriptcommandcompletecount,
                                                                                 :executionguid,
                                                                                 :executionplanid,
                                                                                 :jobname,
                                                                                 :jobdescription,
                                                                                 :userid,
                                                                                 :optimizername,
                                                                                 :machineos,
                                                                                 :machinename,
                                                                                 :authinfo,
                                                                                 :operationtype)";
            try
            {
                using (OracleCommand insertCommand = new OracleCommand(insertCommandQuery, connection))
                {
                    insertCommand.Parameters.Add(":jobid", osssrvjobid);
                    insertCommand.Parameters.Add(":scheduledstartdate", scheduledstartdate.DateTime);
                    insertCommand.Parameters.Add(":scheduledstartdateutc", scheduledstartdate.UtcDateTime);
                    insertCommand.Parameters.Add(":targetenddate", targetenddate.DateTime);
                    insertCommand.Parameters.Add(":targetenddateutc", targetenddate.UtcDateTime);
                    insertCommand.Parameters.Add(":state", state);
                    insertCommand.Parameters.Add(":script", OracleDbType.Clob).Value = string.Join(Environment.NewLine, scriptCommands.Select(o => o.ScriptLine));
                    insertCommand.Parameters.Add(":scripttype", "script");
                    insertCommand.Parameters.Add(":scriptcommandcount", scriptCommands.Sum(o => o.ChangesCount));
                    insertCommand.Parameters.Add(":scriptcommandcompletecount", OracleDbType.Int64).Value = 0;
                    insertCommand.Parameters.Add(":executionguid", guid.ToByteArray());
                    insertCommand.Parameters.Add(":executionplanid", executionplanid);
                    insertCommand.Parameters.Add(":jobname", jobname);
                    insertCommand.Parameters.Add(":jobdescription", jobdescription);
                    insertCommand.Parameters.Add(":userid", userid);
                    insertCommand.Parameters.Add(":optimizername", optimizername);
                    insertCommand.Parameters.Add(":machineos", versionString);
                    insertCommand.Parameters.Add(":machinename", machineName);
                    insertCommand.Parameters.Add(":authinfo", authinfo);
                    insertCommand.Parameters.Add(":operationtype", operationType);
                    insertCommand.ExecuteNonQuery();
                }
                Log.Debug("Insert completed.");
            }
        }
    }
}
