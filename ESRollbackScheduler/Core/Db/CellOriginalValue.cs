using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRollbackScheduler.Core.Db
{
    internal class CellOriginalValue
    {
        public long PIOssId { get; set; }
        public string PIClid { get; set; }
        public string RadioTechnology { get; set; }
        public long PICellId { get; set; }
        public string Moname { get; set; }
        public string ParameterName { get; set; }
        public string ParameterRealName { get; set; }
        public string ParameterValue { get; set; }
        public string ParameterRealValue { get; set; }
        public string ParameterNewValue { get; set; }
        //public DateTime CreationTime { get; set; }
        public string JobId { get; set; }
        public string ExecutionPlanId { get; set; }
        public string OptimizerId { get; set; }
        public string ModuleName { get; set; }
        //public int? MoNotFoundCount { get; set; } = 0;

        public override string ToString() => $"{nameof(CellOriginalValue)}: PICellId={PICellId}, ParameterRealName={ParameterRealName}";
    }
}
