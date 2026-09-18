using IdeaCanvas.Models;

namespace IdeaCanvas.Interfaces
{
    interface IMindMapService
    {
        public MindMapNode AddNode(MindMap map, Guid parentId, string text);
        public void RemoveNode(MindMap map, Guid nodeId);
        public void MoveNode(MindMap map, Guid nodeId, Guid newParentId, int newIndex);
        public void ReorderSibling(MindMapNode parent, Guid nodeId, int newIndex);
        public void UpdateText(MindMapNode node, string text);
    }
}
