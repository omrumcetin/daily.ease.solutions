using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRollbackScheduler
{
    internal static class AppConfig
    {
        public static string OracleDbConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["piworks-db"].ConnectionString; }
        }

        public static string OriginalValuesSqlitePath
        {
            get { return ConfigurationManager.AppSettings["OriginalValuesSqlitePath"].ToString(); }
        }

        public static int ServiceSleepInMinutes
        {
            get { return int.Parse(ConfigurationManager.AppSettings["ServiceSleepInMinutes"]); }
        }
    }
}
