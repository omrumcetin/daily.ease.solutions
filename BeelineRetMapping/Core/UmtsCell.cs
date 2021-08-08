namespace BeelineRetMapping.Core
{
    internal sealed class UmtsCell
    {
        public string CellName { get; set; }
        public int CellId { get; set; }
        public int? LAC { get; set; }
        public int? UtranCellIdentity { get; set; }
        public int? OssId { get; set; }
        public Ret RetMap { get; set; }
    }
}
