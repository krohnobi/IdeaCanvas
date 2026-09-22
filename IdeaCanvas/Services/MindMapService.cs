using IdeaCanvas.Interfaces;
using IdeaCanvas.Models;

namespace IdeaCanvas.Services
{
    public class MindMapService : IMindMapService
    {

        public MindMap CreateMap(string title)
        {
            return new MindMap { Title = title };
        }

        public MindMapNode AddNode(MindMap map, Guid? parentId, string text)
        {

            if (parentId == null && map.Nodes.Any(n => n.ParentId == null))
                throw new InvalidOperationException("Root-Node already exists.");

            var siblings = GetChildren(map, parentId);


            MindMapNode newNode = new MindMapNode
            {
                Id = Guid.NewGuid(),
                Text = text,
                ParentId = parentId,
            };
            newNode.SortOrder = siblings.Count == 0 ? 0 : siblings.Max(s => s.SortOrder) + 1;

            map.Nodes.Add(newNode);
            map.ModifiedAt = DateTime.Now;
            return newNode;
        }

        public void UpdateDescription(MindMap map, Guid nodeId, string description)
        {
            MindMapNode? node = GetNodeById(map, nodeId);
            if (node == null)
            {
                throw new InvalidOperationException($"Node {nodeId} not found.");
            }

            node.Description = description;
            map.ModifiedAt = DateTime.Now;
        }

        public void UpdateText(MindMap map, Guid nodeId, string text)
        {
            MindMapNode? node = GetNodeById(map, nodeId);
            if (node == null)
            {
                throw new InvalidOperationException($"Node {nodeId} not found.");
            }

            node.Text = text;
            map.ModifiedAt = DateTime.Now;
        }

        public bool RemoveNode(MindMap map, Guid nodeId)
        {
            var node = GetNodeById(map, nodeId);
            if (node == null)
                return false;

            var idsToRemove = GetSubtreeIds(map, nodeId).ToHashSet();
            map.Nodes.RemoveAll(n => idsToRemove.Contains(n.Id));

            map.ModifiedAt = DateTime.Now;
            return true;
        }

        public void UnpinNode(MindMap map, Guid nodeId)
        {
            var node = GetNodeById(map, nodeId);
            if (node == null)
                throw new InvalidOperationException($"Node {nodeId} not found.");
            node.Layout.IsPinned = false;
            map.ModifiedAt = DateTime.Now;
        }

        public bool MoveNode(MindMap map, Guid nodeId, Guid newParentId, int newSortOrder)
        {

            var node = GetNodeById(map, nodeId);
            if (node == null)
                throw new InvalidOperationException($"Node {nodeId} not found.");

            if (node.ParentId == null)
                throw new InvalidOperationException("Root-Node can not be moved.");

            var newParent = GetNodeById(map, newParentId);
            if (newParent == null)
                throw new InvalidOperationException($"Target-Parent {newParentId} not found.");

            var subtreeIds = GetSubtreeIds(map, nodeId).ToHashSet();
            if (subtreeIds.Contains(newParentId))
                throw new InvalidOperationException("Cannot move a node to one of its own descendants.");

            node.ParentId = newParentId;
            node.SortOrder = newSortOrder;
            map.ModifiedAt = DateTime.Now;
            return true;
        }

        public void ReorderSiblings(MindMap map, Guid? parentId, IEnumerable<Guid> orderedNodeIds)
        {
            int sortOrder = 0;
            foreach (var nodeId in orderedNodeIds)
            {
                var node = GetNodeById(map, nodeId);
                if (node == null)
                    throw new InvalidOperationException($"Node {nodeId} not found.");

                if (node.ParentId != parentId)
                    throw new InvalidOperationException($"Node {nodeId} does not belong to Parent {parentId}.");

                node.SortOrder = sortOrder;
                sortOrder++;
            }

            map.ModifiedAt = DateTime.Now;
        }




        private MindMapNode? GetNodeById(MindMap map, Guid id)
        {
            return map.Nodes.FirstOrDefault(x => x.Id == id);
        }

        private List<MindMapNode> GetChildren(MindMap map, Guid? parentId)
        {
            return map.Nodes.Where(n => n.ParentId == parentId).OrderBy(n => n.SortOrder).ToList();
        }

        private IEnumerable<Guid> GetSubtreeIds(MindMap map, Guid rootId)
        {
            var result = new List<Guid>();
            var queue = new Queue<Guid>();
            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                result.Add(currentId);

                var children = GetChildren(map, currentId);
                foreach (var child in children)
                {
                    queue.Enqueue(child.Id);
                }
            }

            return result;
        }
    }
}