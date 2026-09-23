using IdeaCanvas.Models;

namespace IdeaCanvas.Interfaces
{
    interface IMindMapService
    {
        public MindMapNode AddNode(MindMap map, Guid? parentId, string text);
        public bool RemoveNode(MindMap map, Guid nodeId);
        public bool MoveNode(MindMap map, Guid nodeId, Guid newParentId, int newSortOrder);
        public void ReorderSiblings(MindMap map, Guid? parentId, IEnumerable<Guid> orderedNodeIds);
        public void UpdateText(MindMap map, Guid nodeId, string text);
        public void UpdateDescription(MindMap map, Guid nodeId, string description);
        public void UnpinNode(MindMap map, Guid nodeId);
        public bool ExportPdf(string pngBase64, string targetPdfPath);
    }
}
