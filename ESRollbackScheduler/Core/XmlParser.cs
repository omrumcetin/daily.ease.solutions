using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ESRollbackScheduler.Core
{
    internal static class XmlParser
    {
        public static void Parse(string MultiSchedules, out (DateTime?, string) RollbackSchedulePair)
        {
            RollbackSchedulePair = (null, null);
            using (StringReader reader = new StringReader(MultiSchedules)) 
            {

                XmlSerializer serializer = new XmlSerializer(typeof(ArrayOfSchedule));
                ArrayOfSchedule arrayOfSchedule = (ArrayOfSchedule)serializer.Deserialize(reader);
                foreach (var schedule in arrayOfSchedule.Schedulers)
                {
                    if (schedule.Id == "ForceRollback")
                    {
                        if(DateTime.TryParse(schedule.EndDateLocal, out var endDateLocal))
                            RollbackSchedulePair = (endDateLocal, schedule.WindowsTimezoneId);
                    }
                }
            }
        }
    }

}
