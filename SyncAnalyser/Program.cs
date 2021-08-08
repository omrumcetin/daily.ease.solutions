using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace SyncAnalyser
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .MinimumLevel.Debug()
                    .CreateLogger();

            string sourceFileMask = null;
            string sqliteDbName = null;
            Stopwatch stopWatch = new Stopwatch();

            if (args.Length == 2)
            {
                sourceFileMask = args[1];
            }
            else if (args.Length <= 1)
            {
                try
                {
                    stopWatch.Reset();
                    stopWatch.Start();
                    sourceFileMask = DbContextHelper.CollectLogsFromDb() + "*.log";
                    Log.Debug($"Completed in {stopWatch.ElapsedMilliseconds} ms.");
                    if (!string.IsNullOrEmpty(args[0]))
                        sqliteDbName = args[0];
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    Environment.Exit(0);
                }
            }
            else
            {
                Log.Error("Proper usage <SynchLogPath> <SqliteFileName.sqlite>");
                Log.Error("             ex : all_logs.sqlite *.log");
                Environment.Exit(0);
            }
            if (!string.IsNullOrEmpty(sqliteDbName))
            {
                foreach (string filePath in Directory.EnumerateFiles(".", sourceFileMask))
                {
                    Parser.ParseFile(filePath, out var AnalysedBlockList);
                    var sourceName = Path.GetFileNameWithoutExtension(filePath);
                    Log.Debug($"Analysing {sourceName}...");
                    stopWatch.Reset();
                    stopWatch.Start();
                    DbContextHelper.CreateSqliteDbAndInsertData(AnalysedBlockList, sqliteDbName, sourceName);
                    stopWatch.Stop();
                    Log.Debug($"Done in {stopWatch.ElapsedMilliseconds} ms");
                }
            }
        }
    }
}
