using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SyncAnalyser
{
    class QueryStats
    {
        public DateTime Time { get; }
        public string QueryLog { get; }
        public string Value { get; }

        public QueryStats(string logLine)
        {
            var match = Regex.Match(logLine, @".*\[([0-9 :.]+)\]\s(.*)\:\s(.*)");
            Time = DateTime.ParseExact(match.Groups[1].Value, "yyyyMMdd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            QueryLog = match.Groups[2].Value;
            Value = match.Groups[3].Value;
        }
    }
}
