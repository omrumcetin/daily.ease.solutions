namespace BeelineRetMapping.Core
{
    internal sealed class LteCell
    {
        public string CellName { get; set; }
        public int CellId { get; set; }
        public int ENodebId { get; set; }
        public int CI { get; set; }
        public int OssId { get; set; }
        public Ret RetMap = new Ret();
    }
}
