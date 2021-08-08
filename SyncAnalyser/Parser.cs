using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SyncAnalyser
{
    internal static class Parser
    {
        private readonly static string[] querySegmentKeys = { "UPDATE", "SELECT", "MERGE" };
        public static void ParseFile(string filePath, out List<AnalysedBlock> AnalysedBlockList)
        {
            AnalysedBlockList = new List<AnalysedBlock>();

            using (StreamReader sourceFile = new StreamReader(filePath))
            {
                string segmentNumber = null;
                int queryOrder = 0;
                string currentLine;

                while ((currentLine = sourceFile.ReadLine()) != null)
                {
                    if (currentLine.Contains("- 'S", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var match = Regex.Match(currentLine, @"(\w+)");
                        segmentNumber = match.Groups[0].Value;
                    }
                    else if (querySegmentKeys.Any(x => currentLine.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        AnalysedBlock logBlock = new AnalysedBlock();
                        var exitProcessing = !ProcessQueryBlock(sourceFile, currentLine, out logBlock);
                        logBlock.QueryOrder = queryOrder++;
                        logBlock.Segment = segmentNumber;
                        AnalysedBlockList.Add(logBlock);
                        if (exitProcessing)
                            break;
                    }
                }
            }
        }

        private static bool ProcessQueryBlock(StreamReader sourceFile, string currentLine, out AnalysedBlock block)
        {
            block = new AnalysedBlock();

            string tempFullQueryBlock = currentLine;
            while (!string.IsNullOrEmpty(currentLine = sourceFile.ReadLine()))
            {
                tempFullQueryBlock = tempFullQueryBlock + System.Environment.NewLine + currentLine;
                if (currentLine.EndsWith(';'))
                    break;
            }
            block.Query = tempFullQueryBlock;
            return ProcessInfoBlock(sourceFile, block);
        }

        private static bool ProcessInfoBlock(StreamReader sourceFile, AnalysedBlock segment)
        {
            string currentLine;
            while ((currentLine = sourceFile.ReadLine()) != null)
            {
                if (currentLine.Contains("Rows affected", StringComparison.InvariantCultureIgnoreCase))
                {
                    QueryStats queryLog = new QueryStats(currentLine);
                    segment.RowsAffected = Convert.ToInt32(queryLog.Value);
                    segment.LogTimeStamp = queryLog.Time;
                }
                else if (currentLine.Contains("Duration in seconds", StringComparison.InvariantCultureIgnoreCase))
                {
                    QueryStats queryLog = new QueryStats(currentLine);
                    segment.Duration = Convert.ToDouble(queryLog.Value);
                }
                else if (currentLine.Contains("Redo usage", StringComparison.InvariantCultureIgnoreCase))
                {
                    QueryStats queryLog = new QueryStats(currentLine);
                    segment.TotalRedoUsage = Convert.ToDouble(queryLog.Value);
                }
                else if (currentLine.Contains("Undo usage", StringComparison.InvariantCultureIgnoreCase))
                {
                    QueryStats queryLog = new QueryStats(currentLine);
                    segment.TotalUndoUsage = Convert.ToDouble(queryLog.Value);
                    return true;
                }
                else if (currentLine.Contains("Exception occurred", StringComparison.InvariantCultureIgnoreCase))
                {
                    currentLine = sourceFile.ReadLine();
                    QueryStats queryLog = new QueryStats(currentLine);
                    segment.ErrorLog = queryLog.Value;
                    return false;
                }
            }
            return true;
        }
    }
}
