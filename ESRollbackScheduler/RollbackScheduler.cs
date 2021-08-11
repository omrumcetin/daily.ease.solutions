using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ESRollbackScheduler.Core;
using ESRollbackScheduler.Core.Db;

namespace ESRollbackScheduler
{
    class RollbackScheduler
    {
        public static void Run()
        {
            while (true)
            {
                Log.Information("Started checking ES Jobs");
                List<RollbackConfig> energySavingJobsConfigCache = new List<RollbackConfig>();
                List<CellOriginalValue> cellsOriginalValuesCache = new List<CellOriginalValue>();
                List<OssJob> esOssJobsCache = new List<Core.OssJob>();

                try
                {
                    energySavingJobsConfigCache = DbProxy.GetEnergySavingJobConfig();
                    cellsOriginalValuesCache = DbProxy.GetCellsOriginalValues();
                    esOssJobsCache = DbProxy.GetEnergySavingOssRollbackJobs();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    Thread.Sleep(AppConfig.ServiceSleepInMinutes * 60 * 1000);
                    continue;
                }

                List<int> candidateExecutionPlanIds = new List<int>();
                foreach (var energySavingJobConfig in energySavingJobsConfigCache)
                {
                    if (DateTime.Now >= energySavingJobConfig.EndDateLocal.Value.AddHours(1) && DateTime.Now < energySavingJobConfig.EndDateLocal.Value.AddHours(3))
                    {
                        candidateExecutionPlanIds.Add(energySavingJobConfig.ExecutionPlanId);
                    }
                }
                if (candidateExecutionPlanIds.Count == 0)
                {
                    Log.Information("Nothing to evaluate");
                    Log.Information("Completed checking ES Jobs");
                    Thread.Sleep(AppConfig.ServiceSleepInMinutes * 60 * 1000);
                    continue;
                }

                Log.Debug($"Execution plan ids will be evaluate are : {string.Join(", ", candidateExecutionPlanIds)}");
                foreach (var candidateExecutionPlanId in candidateExecutionPlanIds)
                {
                    Log.Debug($"Evaluating execution plan id {candidateExecutionPlanId}");
                    List<string> scriptCommands = new List<string>();
                    var lockedCellsByExecutionPlanId = cellsOriginalValuesCache.Where(x => x.ExecutionPlanId == candidateExecutionPlanId
                                                                                        && (x.ParameterRealName.ToUpper() == "OPERSTATE" || x.ParameterRealName.ToUpper() == "ADMINSTATE")
                                                                                        && x.ParameterRealValue == "1");
                    if (lockedCellsByExecutionPlanId.Count() == 0)
                    {
                        Log.Debug($"All cells unlocked already! Nothing to do.");
                        continue;
                    }
                    foreach (var cell in lockedCellsByExecutionPlanId)
                    {
                        Log.Debug($"Creating unlock command for {cell}");
                        string command = $"modcell({cell.PICellId},{cell.ParameterRealName}=0)";
                        scriptCommands.Add(command);
                    }
                    Core.DbProxy.CreateOssJob(scriptCommands, esOssJobsCache, candidateExecutionPlanId);
                }
                Log.Information("Completed checking ES Jobs");
                Thread.Sleep(AppConfig.ServiceSleepInMinutes * 60 * 1000);
            }
        }
    }
}
