namespace IdeaCanvas.Models
{
    public class EdgeLayout
    {
        #region Properties
        public Guid SourceNodeId { get; set; }
        public Guid TargetNodeId { get; set; }
        public List<EdgePoint> Points { get; set; } = new();
        #endregion
    }
}