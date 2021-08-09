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
        public static List<OssJob> GetEnergySavingRollbackJob()
        {
            Log.Debug("Getting energy saving rollbacking job from db.");
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
                Log.Debug($@"Evaluating following execution plan ids : {string.Join(",", ossJobs.Select(x => x.ExecutionPlanId.ToString())
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

        public static void GetCellsOriginalValues(int ExecutionPlanId)
        {
            using (var connection = new SQLiteConnection(AppConfig.OriginalValuesSqlitePath))
            {
                using (var command = new SQLiteCommand(@"
                            SELECT *
                                FROM CellOriginalValues
                              WHERE EXECUTIONPLANID = @executionplanid",
                      connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@executionplanid", ExecutionPlanId);
                    ReadOriginalValueQuery(command, out var CellOriginalValue);
                }
            }
        }

        private static void ReadOriginalValueQuery(SQLiteCommand command, out object cellOriginalValue)
        {
            throw new NotImplementedException();
        }

        public static void CreateOssJob()
        {
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
            //try
            //{
            //    using (OracleCommand insertCommand = new OracleCommand(insertCommandQuery, connection))
            //    {
            //        //insertCommand.Parameters.Add(":jobid", osssrvjobid);
            //        //insertCommand.Parameters.Add(":scheduledstartdate", scheduledstartdate.DateTime);
            //        //insertCommand.Parameters.Add(":scheduledstartdateutc", scheduledstartdate.UtcDateTime);
            //        //insertCommand.Parameters.Add(":targetenddate", targetenddate.DateTime);
            //        //insertCommand.Parameters.Add(":targetenddateutc", targetenddate.UtcDateTime);
            //        //insertCommand.Parameters.Add(":state", state);
            //        //insertCommand.Parameters.Add(":script", OracleDbType.Clob).Value = string.Join(Environment.NewLine, scriptCommands.Select(o => o.ScriptLine));
            //        //insertCommand.Parameters.Add(":scripttype", "script");
            //        //insertCommand.Parameters.Add(":scriptcommandcount", scriptCommands.Sum(o => o.ChangesCount));
            //        //insertCommand.Parameters.Add(":scriptcommandcompletecount", OracleDbType.Int64).Value = 0;
            //        //insertCommand.Parameters.Add(":executionguid", guid.ToByteArray());
            //        //insertCommand.Parameters.Add(":executionplanid", executionplanid);
            //        //insertCommand.Parameters.Add(":jobname", jobname);
            //        //insertCommand.Parameters.Add(":jobdescription", jobdescription);
            //        //insertCommand.Parameters.Add(":userid", userid);
            //        //insertCommand.Parameters.Add(":optimizername", optimizername);
            //        //insertCommand.Parameters.Add(":machineos", versionString);
            //        //insertCommand.Parameters.Add(":machinename", machineName);
            //        //insertCommand.Parameters.Add(":authinfo", authinfo);
            //        //insertCommand.Parameters.Add(":operationtype", operationType);
            //        insertCommand.ExecuteNonQuery();
            //    }
            //    Log.Debug("Insert completed.");
            //}
        }
    }
}
