using Serilog;
using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace ESRollbackScheduler
{
    public partial class ESRollbackSchedulerService : ServiceBase
    {
        public ESRollbackSchedulerService()
        {
            InitializeComponent();

            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
            var logFileName = $"log_{DateTime.Today.ToString("yyyyMMdd")}.txt";
            Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File($"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")}/{logFileName}")
                    .MinimumLevel.Debug()
                    .CreateLogger();
        }

        protected override void OnStart(string[] args)
        {
            Log.Information("ES Rollback scheduler has been started");
            Thread thread = new Thread(RollbackScheduler.Run) { IsBackground = true };
            thread.Start();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStop()
        {
            Log.Information("ES Rollback scheduler has been stopped");
        }
    }
}
