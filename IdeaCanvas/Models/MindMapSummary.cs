namespace IdeaCanvas.Models
{
    public class MindMapSummary
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime CreatedAt { get; set; };
        public int NodeCount { get; set; }
    }
}
