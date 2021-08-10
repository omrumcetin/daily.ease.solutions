using System.Xml.Serialization;

namespace ESRollbackScheduler.Core
{
    [XmlRoot("ArrayOfSchedule", Namespace = "http://piworks.net/PISON/SharedDomain", IsNullable = false)]
    public class ArrayOfSchedule
    {
        [XmlElement("Schedule")]
        public Schedule[] Schedulers;
    }

    public class Schedule
    {
        [XmlElement("Id")]
        public string Id;
        [XmlElement("EndDateLocal")]
        public string EndDateLocal;
        [XmlElement("WindowsTimezoneId")]
        public string WindowsTimezoneId;
    }
}
