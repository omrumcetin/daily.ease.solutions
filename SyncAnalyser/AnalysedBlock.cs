using System;

namespace SyncAnalyser
{
    class AnalysedBlock
    {
        public string Segment { get; set; }
        public int QueryOrder { get; set; }
        public string Query { get; set; }
        public DateTime LogTimeStamp { get; set; }
        public int RowsAffected { get; set; }
        public double Duration { get; set; }
        public double TotalRedoUsage { get; set; }
        public double TotalUndoUsage { get; set; }
        public string ErrorLog { get; set; }
    }
}
