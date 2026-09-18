using IdeaCanvas.Interfaces;
using IdeaCanvas.Models;

namespace IdeaCanvas.Services
{
    public class MindMapService : IMindMapService
    {
        public MindMapNode AddNode(MindMap map, Guid parentId, string text)
        {
            return new();
        }
        public void RemoveNode(MindMap map, Guid nodeId) { }
        public void MoveNode(MindMap map, Guid nodeId, Guid newParentId, int newIndex) { }
        public void ReorderSibling(MindMapNode parent, Guid nodeId, int newIndex) { }
        public void UpdateText(MindMapNode node, string text) { }

        private static MindMapNode? FindNode(MindMapNode root, Guid id)
        {
            if (root.Id == id)
            {
                return root;
            }

            foreach (MindMapNode child in root.Children)
            {
                MindMapNode? foundNode = FindNode(child, id);

                if (foundNode != null)
                    return foundNode;
            }
            return null;
        }
        private MindMapNode? FindParent(MindMapNode root, Guid childId)
        {
            foreach(MindMapNode child in root.Children)
            {
                if(child.Id == childId)
                {
                    return root;
                }
            }

            foreach(MindMapNode child in root.Children)
            {
                MindMapNode? foundNode = FindParent(child, childId);
                if (foundNode!=null)
                {
                    return foundNode;
                }
            }
            return null;
        }
    }
}