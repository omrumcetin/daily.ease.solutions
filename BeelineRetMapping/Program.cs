using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace BeelineRetMapping
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
            var logFileName = $"log_{DateTime.Today.ToString("yyyyMMdd")}.txt";
            Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File($"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")}/{logFileName}")
                    .MinimumLevel.Debug()
                    .CreateLogger();

            MapRets();
        }
        static void MapRets()
        {
            List<Core.UmtsCell> umtsCells = DbHelper.FetchUmtsCells();
        }
    }
}
