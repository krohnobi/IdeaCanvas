namespace IdeaCanvas.Models
{
    public class MindMapNode
    {
        #region Properties
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentId { get; set; }   // null = Root
        public int SortOrder { get; set; }
        public NodeLayout Layout { get; set; } = new();
        #endregion
    }
}
