using IdeaCanvas.Models;

namespace IdeaCanvas.Interfaces
{
    interface IMindMapService
    {
        MindMapNode AddNode(MindMap map, Guid? parentId, string text);
        bool RemoveNode(MindMap map, Guid nodeId);
        bool MoveNode(MindMap map, Guid nodeId, Guid newParentId, int newSortOrder);
        void ReorderSiblings(MindMap map, Guid? parentId, IEnumerable<Guid> orderedNodeIds);
        void UpdateText(MindMap map, Guid nodeId, string text);
        void UpdateDescription(MindMap map, Guid nodeId, string description);
        void UnpinNode(MindMap map, Guid nodeId);
        void UnpinAllNodes(MindMap map);
        int GetExistingLevels(MindMap map);
        bool SetLevelColor(MindMap map, int level, string hexColor);
        string? GetLevelColor(MindMap map, int level);
        string GetTextColorForLevel(MindMap map, int level);
        public bool RemoveLevelColor(MindMap map, int level);
        public void ResetLevelColor(MindMap map);
    }
}
