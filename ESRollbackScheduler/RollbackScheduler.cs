using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ESRollbackScheduler
{
    class RollbackScheduler
    {
        public static void Run()
        {
            while (true)
            {
                var energySavingJobsConfigCache = Core.DbProxy.GetEnergySavingJobConfig();
                var cellsOriginalValuesCache = Core.DbProxy.GetCellsOriginalValues();

                List<int> candidateExecutionPlanIds = new List<int>();
                foreach (var energySavingJobConfig in energySavingJobsConfigCache)
                {
                    if (DateTime.Now >= energySavingJobConfig.EndDateLocal.Value.AddHours(1))
                    {
                        candidateExecutionPlanIds.Add(energySavingJobConfig.ExecutionPlanId);
                    }
                }
                if (candidateExecutionPlanIds.Count == 0)
                {
                    Thread.Sleep(30 * 60 * 1000);
                }
                foreach (var candidateExecutionPlanId in candidateExecutionPlanIds)
                {
                    var lockedCellsByExecutionPlanId = cellsOriginalValuesCache.Where(x => x.ExecutionPlanId == candidateExecutionPlanId 
                                                                                        && (x.ParameterRealName.ToUpper() == "OPERSTATE" || x.ParameterRealName.ToUpper() == "ADMINSTATE")
                                                                                        && x.ParameterRealValue == "1");
                    foreach (var cell in lockedCellsByExecutionPlanId)
                    {

                    }
                }

            }
        }
    }
}
