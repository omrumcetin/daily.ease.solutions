using Microsoft.Data.Sqlite;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HwiRetMinMaxSetter
{
    class Program
    {
        const string DB_CONNECTION_STRING = "Data Source=10.0.128.32:11521/MNP;User Id=MNP;Password=mnp;Validate Connection=true;";
        static void Main(string[] args)
        {
            List<RetDeviceData> retDeviceDataList = new List<RetDeviceData>();
            string[] files =
                Directory.GetFiles(".", "*.sqlite", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var db = $"DataSource={Path.GetFileName(file)}";
                DataTable dt = new DataTable();
                using (var connection = new SqliteConnection(db))
                {
                    connection.Open();
                    var query =
                    @"
                        SELECT *
                        FROM RetDeviceData
                    ";

                    using (SqliteCommand cmd = new SqliteCommand(query, connection))
                    {
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                var retDeviceData = new RetDeviceData
                                {
                                    NeFdn = rdr.GetString(0),
                                    NeName = rdr.GetString(1),
                                    DeviceNo = rdr.GetInt32(2),
                                    MaxTilt = rdr.IsDBNull(3) ? default(int) : rdr.GetInt32(3),
                                    MinTilt = rdr.IsDBNull(4) ? default(int) : rdr.GetInt32(4)
                                };
                                retDeviceDataList.Add(retDeviceData);
                            }
                        }
                    }
                }
            }
            List<HwiCell> hwiCells = new List<HwiCell>();
            using (OracleConnection connection = new OracleConnection(DB_CONNECTION_STRING))
            {
                connection.Open();
                string cmdQueryText = @$"
                                            SELECT  CELLID,
                                                    RETOSSID,
                                                    RETMONAME
                                                FROM ALL_CELLS
                                              WHERE CMS_PK_1 LIKE '%HWI%'";
                using (OracleCommand command = new OracleCommand(cmdQueryText, connection))
                {
                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        reader.FetchSize = reader.RowSize * 100000;
                        while (reader.Read())
                        {
                            var hwiCell = new HwiCell
                            {
                                CellId = reader.GetInt32(0),
                                RetOssId = reader.IsDBNull(1) ? default(int) : reader.GetInt32(1),
                                RetMoname = reader.IsDBNull(2) ? default(string) : reader.GetString(2)
                            };
                            hwiCells.Add(hwiCell);
                        }
                    }
                }
                OracleCommand cmd = connection.CreateCommand();

                int commitCounter = 0;
                var hwiCellsWithMoname = hwiCells.Where(x => !string.IsNullOrEmpty(x.RetMoname)).ToList();

                List<CombinedRetSource> fullRetList = new List<CombinedRetSource>();
                // update all_cells
                foreach (var retDeviceData in retDeviceDataList)
                {
                    commitCounter++;
                    var hwiCell = hwiCellsWithMoname.Where(x => x.RetMoname.Contains(retDeviceData.NeName) && x.RetMoname.Contains($"RETSUBUNIT:DEVICENO={retDeviceData.DeviceNo}"));
                    if (!hwiCell.Any())
                    {
                        continue;
                    }
                    if (retDeviceData.MaxTilt < 50 || retDeviceData.MaxTilt == null || retDeviceData.MinTilt == null)
                        continue;
                    var fullRet = new CombinedRetSource { 
                                            CellIds = string.Join(",", hwiCell.Select(x => x.CellId).ToList()), 
                                            MinTilt = retDeviceData.MinTilt, 
                                            MaxTilt = retDeviceData.MaxTilt };
                    fullRetList.Add(fullRet);
                }
                var groupedRetList = fullRetList.GroupBy(p => new { p.MinTilt, p.MaxTilt })
                                                .Select(g => new { MinTilt = g.Key.MinTilt,
                                                    MaxTilt = g.Key.MaxTilt,
                                                    CellIdList = string.Join(",", g.Select(e => e.CellIds))});
                // query recorder
                string fileName = "queryRecords.sql";
                foreach (var groupedRed in groupedRetList)
                {
                    var cellCount = Regex.Matches(groupedRed.CellIdList, ",").Count();
                    var loopCount = cellCount / 1000;
                    var leftOver = cellCount % 1000;
                    for (var i = 0; i <= loopCount; i++)
                    {
                        var takeAmount = i != loopCount ? 1000 : leftOver;
                        var newCellIdList = string.Join(",",groupedRed.CellIdList.Split(',').Skip(i * 1000).Take(takeAmount));
                        if (string.IsNullOrEmpty(newCellIdList))
                            continue;
                        var updateQuery = $"UPDATE ALL_CELLS SET MAXTILT = :MaxTilt, MINTILT =:MinTilt WHERE CELLID in ({newCellIdList})";
                        cmd.CommandText = updateQuery;
                        cmd.Parameters.Clear();
                        cmd.Parameters.Add("MaxTilt", OracleDbType.Decimal, groupedRed.MaxTilt, ParameterDirection.Input);
                        cmd.Parameters.Add("MinTilt", OracleDbType.Decimal, groupedRed.MinTilt, ParameterDirection.Input);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"Updated -> MinTilt = {groupedRed.MinTilt} and MaxTilt = {groupedRed.MaxTilt} for CellId = {newCellIdList}");
                        File.AppendAllText(fileName,$"UPDATE ALL_CELLS SET MAXTILT = {groupedRed.MaxTilt}, MINTILT ={groupedRed.MinTilt} WHERE CELLID in ({newCellIdList});" + Environment.NewLine);
                    }
                }
            }
        }
    }


    internal sealed class RetDeviceData
    {
        public string NeFdn { get; set; }
        public string NeName { get; set; }
        public int DeviceNo { get; set; }
        public int? MaxTilt { get; set; }
        public int? MinTilt { get; set; }
    }

    internal sealed class HwiCell
    {
        public int CellId { get; set; }
        public string? RetMoname { get; set; }
        public int? RetOssId { get; set; }
    }

    internal sealed class CombinedRetSource
    {
        public string CellIds { get; set; }
        public int? MinTilt { get; set; }
        public int? MaxTilt { get; set; }
    }
}
