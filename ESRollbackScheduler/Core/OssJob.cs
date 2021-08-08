using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRollbackScheduler.Core
{
    internal sealed class OssJob
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
        public int JobId { get; set; }
        public DateTime ScheduledStartDate { get; set; }
        public DateTime TargetEndDate { get; set; }
        public DateTime ActualStartDate { get; set; }
        public DateTime ActualEndDate { get; set; }
        public string State { get; set; }
        public Guid ExecutionGuid { get; set; }
        public string UserId { get; set;}
        public string UserName { get; set; }
        public string JobDescription { get; set; }
        public int ExecutionPlanId { get; set; }
        public int OperationType { get; set; }
    }
}
