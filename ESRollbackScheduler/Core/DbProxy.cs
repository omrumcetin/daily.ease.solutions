﻿using Oracle.ManagedDataAccess.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRollbackScheduler.Core
{
    internal static class DbProxy
    {
        public static void GetEnergySavingRollbackJob()
        {
            //OSS.JOBID,
            //OSS.SCHEDULEDSTARTDATE,
            //OSS.TARGETENDDATE,
            //OSS.ACTUALSTARTDATE,
            //OSS.ACTUALENDDATE,
            //OSS.STATE,
            //OSS.EXECUTIONGUID,
            //U.ID USERID,
            //U.NAME USERNAME,
            //OSS.JOBDESCRIPTION,
            //OSS.EXECUTIONPLANID,
            //OSS.OPERATIONTYPE
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
                            AND SERVICETYPE IN ('3G Energy Saving', '4G Energy Saving')
                            AND OPERATIONTYPE = 1",
                        connection)
            )
            {
                connection.Open();
                //ReadOssJob(command, ossJob)
            }
        }

        public static void GetCellsOriginalValues()
        {

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