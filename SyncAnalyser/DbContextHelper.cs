using Oracle.ManagedDataAccess.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncAnalyser
{
    internal static class DbContextHelper
    {
        public static string CollectLogsFromDb()
        {
            Log.Information($"Reading configuration");
            var config = new AppConfig();

            string folderRelativePath = $"CollectedSynchLogs/{DateTime.Now.ToString("yyyyMMdd")}/";
            if (Directory.Exists(folderRelativePath))
                Directory.Delete(folderRelativePath, true);
            Directory.CreateDirectory(folderRelativePath);

            if (config.DbConnectionString == "FILL_HERE")
                throw new ApplicationException("Need to fill connection string, otherwise input .log files manually");
            Log.Debug($"Collecting {config.SynchResult} sync logs with the day offset {config.DayOffSet} from {config.DbConnectionString}");
            using (OracleConnection connection = new OracleConnection(config.DbConnectionString))
            {
                connection.Open();
                string cmdQueryText = @$"
                                            SELECT SOURCE, EXECUTIONLOG
                                                FROM (
                                                    select  SYNCHNAME||'.'||SOURCENAME SOURCE,
                                                            EXECUTIONLOG,
                                                            ROW_NUMBER() OVER (PARTITION BY SOURCENAME ORDER BY ACTUALSYNCHTIMESTAMP DESC) ROW_NUM 
                                                        from log_cmsquery
                                                      where resultcode = '{config.SynchResult}'
                                                        and datatimestamp >= trunc(sysdate - {config.DayOffSet})
                                                        and datatimestamp < trunc(sysdate + 1 - {config.DayOffSet}))
                                                WHERE ROW_NUM = 1";
                using (OracleCommand command = new OracleCommand(cmdQueryText, connection))
                {
                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fileName = reader.GetString(0);
                            string relativeFileName = $"{folderRelativePath}{fileName}.log";
                            string content = reader.GetString(1);
                            File.WriteAllText(relativeFileName, content);
                        }
                    }
                }
            }
            return folderRelativePath;
        }
        public static void CreateSqliteDbAndInsertData(List<AnalysedBlock> AnalysedBlockList, string sqliteDbPath, string sourceName)
        {
            SQLiteConnectionStringBuilder connectionStringBuilder =
                new SQLiteConnectionStringBuilder
                {
                    DataSource = sqliteDbPath
                };
            string connectionString = connectionStringBuilder.ConnectionString;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = @$"create table [{sourceName}] (    SEGMENT TEXT,
                                                                            QUERYORDER INTEGER,
                                                                            QUERY BLOB,
                                                                            [LOG TIMESTAMP] TEXT,
                                                                            [ROWS AFFECTED] INT,
                                                                            [DURATION] REAL,
                                                                            [TOTAL REDO USAGE] REAL,
                                                                            [TOTAL UNDO USAGE] REAL,
                                                                            [ERROR LOG] TEXT
                                                                            )";
                    command.ExecuteNonQuery();
                }
                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = @$"INSERT INTO [{sourceName}] values (@segment, @queryorder, @query, @logtimestamp, @rowsaffected, @duration, @redousagetotal, @undousagetotal, @errorlog)";
                    foreach (var singleSegment in AnalysedBlockList)
                    {
                        command.Parameters.AddWithValue("@segment", singleSegment.Segment);
                        command.Parameters.AddWithValue("@queryorder", singleSegment.QueryOrder);
                        command.Parameters.AddWithValue("@query", singleSegment.Query);
                        command.Parameters.AddWithValue("@logtimestamp", singleSegment.LogTimeStamp);
                        command.Parameters.AddWithValue("@rowsaffected", singleSegment.RowsAffected);
                        command.Parameters.AddWithValue("@duration", singleSegment.Duration);
                        command.Parameters.AddWithValue("@redousagetotal", singleSegment.TotalRedoUsage);
                        command.Parameters.AddWithValue("@undousagetotal", singleSegment.TotalUndoUsage);
                        command.Parameters.AddWithValue("@errorlog", singleSegment.ErrorLog);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
