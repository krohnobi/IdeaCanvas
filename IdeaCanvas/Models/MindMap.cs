namespace IdeaCanvas.Models
{
    public class MindMap
    {
        #region Properties
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public MindMapNode? RootNode { get; set; }
        #endregion
    }
}
