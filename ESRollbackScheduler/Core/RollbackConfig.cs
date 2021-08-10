using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRollbackScheduler.Core
{
    public class RollbackConfig
    {
        public int ExecutionPlanId { get; set; }
        public DateTime? EndDateLocal { get; set; }
        public string TimeZoneId { get; set; }
    }
}
