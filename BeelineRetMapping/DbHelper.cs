using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;

namespace BeelineRetMapping
{
    internal static class DbHelper
    {
        const string DB_CONNECTION_STRING = "Data Source=10.0.128.32:11521/MNP;User Id=MNP;Password=mnp;Validate Connection=true;";
        public static void FetchCells(out List<Core.UmtsCell> umtsCells, out List<Core.LteCell> lteCells)
        {
            umtsCells = new List<Core.UmtsCell>();
            lteCells = new List<Core.LteCell>();
            using (OracleConnection connection = new OracleConnection(DB_CONNECTION_STRING))
            {
                connection.Open();
                string cmdQueryText = @$"
                                            SELECT /*+ ORDERED PARALLEL(8) */
                                                    AC.CELL,
                                                    AC.CELLID,
                                                    AC.LAC,
                                                    AC.ENODEBID,
                                                    AC.CI,
                                                    AC.RETOSSID,
                                                    AC.RETMONAME,
                                                    AC.ETILT,
                                                    AC.MINTILT,
                                                    AC.MAXTILT,
                                                    AB.OSSID,
                                                    AC.CLID
                                                FROM ALL_CELLS AC
                                              JOIN ALL_BASESTATIONS AB
                                                ON AC.BSID = AB.BSID
                                              JOIN ALL_NODES AN
                                                ON AN.NODEID = AB.NODEID
                                              WHERE CLID IN (321,322)";
                using (OracleCommand command = new OracleCommand(cmdQueryText, connection))
                {
                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        reader.FetchSize = reader.RowSize * 100000;
                        while (reader.Read())
                        {
                            if (reader.GetInt32(11) == 320)
                                continue;
                            else if (reader.GetInt32(11) == 321)
                            {
                                var umtsCell = new Core.UmtsCell()
                                {
                                    CellName = reader.GetString(0),
                                    CellId = reader.GetInt32(1),
                                    LAC = reader.IsDBNull(2) ? default(int) : reader.GetInt32(2),
                                    UtranCellIdentity = reader.IsDBNull(4) ? default(int) : reader.GetInt32(4),
                                    OssId = reader.IsDBNull(10) ? default(int) : reader.GetInt32(10),
                                    RetMap = new Core.Ret()
                                    {
                                        OssId = reader.IsDBNull(5) ? default(int) : reader.GetInt32(5),
                                        Moname = reader.IsDBNull(6) ? default(string) : reader.GetString(6),
                                        ETilt = reader.IsDBNull(7) ? default(int) : reader.GetInt32(7)
                                    }
                                };
                                umtsCells.Add(umtsCell);
                            }
                            else
                            {
                                var lteCell = new Core.LteCell()
                                {
                                    CellName = reader.GetString(0),
                                    CellId = reader.GetInt32(1),
                                    ENodebId = reader.IsDBNull(3) ? default(int) : reader.GetInt32(3),
                                    CI = reader.IsDBNull(4) ? default(int) : reader.GetInt32(4),
                                    OssId = reader.IsDBNull(10) ? default(int) : reader.GetInt32(10),
                                    RetMap = new Core.Ret()
                                    {
                                        OssId = reader.IsDBNull(5) ? default(int) : reader.GetInt32(5),
                                        Moname = reader.IsDBNull(6) ? default(string) : reader.GetString(6),
                                        ETilt = reader.IsDBNull(7) ? default(int) : reader.GetInt32(7)
                                    }
                                };
                                lteCells.Add(lteCell);
                            }
                        }
                    }
                }
            }
        }

        public static void MapUmtsRets(List<Core.UmtsCell> umtsCells)
        {
            using (OracleConnection connection = new OracleConnection(DB_CONNECTION_STRING))
            {
                connection.Open();
                string cmdQueryText = @$"
                                            SELECT  CELL,
                                                    CELLID,
                                                    LAC,
                                                    CI,
                                                    RETOSSID,
                                                    RETMONAME,
                                                    ETILT,
                                                    MINTILT,
                                                    MAXTILT
                                                FROM ALL_CELLS
                                              WHERE CLID = 321";
                using (OracleCommand command = new OracleCommand(cmdQueryText, connection))
                {
                    using (OracleDataReader reader = command.ExecuteReader())
                    {

                    }
                }
            }
        }
    }
}
