using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRollbackScheduler
{
    class RollbackScheduler
    {
        public static void Run()
        {
            var ossJobsCache = Core.DbProxy.GetEnergySavingRollbackJob();
            var cellsOriginalValuesCache = Core.DbProxy.GetCellsOriginalValues();
        }
    }
}
