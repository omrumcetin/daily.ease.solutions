using Microsoft.Extensions.Configuration;

namespace SyncAnalyser
{
    class AppConfig
    {
        IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
        public string DbConnectionString { get; set; }
        public string DayOffSet { get; set; }
        public string SynchResult { get; set; }
        public AppConfig()
        {
            DbConnectionString = config.GetSection("DbConnectionString").Value;
            DayOffSet = config.GetSection("DayOffSet").Value;
            SynchResult = config.GetSection("SynchResult").Value;
        }
    }
}
