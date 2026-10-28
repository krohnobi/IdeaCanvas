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

            //SetAllNodesGeneration(map);

            var siblings = GetChildren(map, parentId);

            int generation = 0;
            if (parentId != null)
            {
                var parent = GetNodeById(map, parentId.Value);
                if (parent == null)
                    throw new InvalidOperationException($"Parent {parentId} not found.");
                generation = parent.Generation + 1;
            }


            MindMapNode newNode = new MindMapNode
            {
                Id = Guid.NewGuid(),
                Text = text,
                ParentId = parentId,
                Generation = generation

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

        public void UnpinAllNodes(MindMap map)
        {
            var pinnedNodes = map.Nodes.Where(x => x.Layout.IsPinned).ToList();
            foreach (var pinnedNode in pinnedNodes)
            {
                pinnedNode.Layout.IsPinned = false;
            }
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

        private MindMapNode? GetNodeById(MindMap map, Guid nodeId)
        {
            return map.Nodes.FirstOrDefault(x => x.Id == nodeId);
        }

        [Obsolete]
        private int GetGeneration(MindMap map, Guid nodeId)
        {
            int generation = 0;
            MindMapNode? currentNode = GetNodeById(map, nodeId);
            if (currentNode == null)
                throw new InvalidOperationException($"Node {nodeId} not found.");

            while (currentNode.ParentId != null)
            {
                currentNode = GetNodeById(map, currentNode.ParentId!.Value); generation++;
                if (currentNode == null)
                    throw new InvalidOperationException("Parent node not found — Baum ist inkonsistent.");
            }

            return generation;
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


        #region Level Color functions

        public int GetExistingLevels(MindMap map)
        {
            if (map.Nodes.Count == 0)
            {
                return 0;
            }

            return map.Nodes.Max(node => node.Generation);
        }
        public bool SetLevelColor(MindMap map, int level, string hexColor)
        {
            bool isValidLevel = level >= 0;
            bool isValidColor = hexColor.StartsWith("#") && hexColor.Length == 7;

            if (!isValidLevel || !isValidColor)
            {
                return false;
            }

            map.LevelColors[level] = hexColor;
            return true;
        }

        public string? GetLevelColor(MindMap map, int level)
        {
            bool isValidLevel = level >= 0;
            if (!isValidLevel)
            {
                return null;
            }

            map.LevelColors.TryGetValue(level, out string? existingColor);
            return existingColor;
        }

        public string GetTextColorForLevel(MindMap map, int level)
        {
            bool isValidLevel = level >= 0;
            if (!isValidLevel)
            {
                return DarkTextColor;
            }

            string? backgroundColor=GetLevelColor(map, level);
            if(backgroundColor != null)
            {
                return GetContrastTextColor(backgroundColor);
            }
            return DarkTextColor;
        }

        public bool RemoveLevelColor(MindMap map, int level)
        {
            bool isValidLevel = level >= 0;
            if (!isValidLevel)
            {
                return false;
            }
            return map.LevelColors.Remove(level);
        }

        public void ResetLevelColor(MindMap map)
        {
            map.LevelColors.Clear();
        }



        #region WCAG Text-Color ratio functions

        private const string DarkTextColor = "#1f2933";
        private const string LightTextColor = "#ffffff";

        // Wählt die Schriftfarbe, die sich am stärksten vom Hintergrund abhebt
        private string GetContrastTextColor(string backgroundColor)
        {
            double backgroundLuminance = GetRelativeLuminance(backgroundColor);
            double darkTextLuminance = GetRelativeLuminance(DarkTextColor);
            double lightTextLuminance = GetRelativeLuminance(LightTextColor);

            double contrastWithDarkText = GetContrastRatio(backgroundLuminance, darkTextLuminance);
            double contrastWithLightText = GetContrastRatio(backgroundLuminance, lightTextLuminance);

            if (contrastWithDarkText >= contrastWithLightText)
            {
                return DarkTextColor;
            }

            return LightTextColor;
        }

        // Helligkeit einer Farbe von 0 (Schwarz) bis 1 (Weiß), so wie das Auge sie wahrnimmt
        private double GetRelativeLuminance(string hexColor)
        {
            string hexDigits = hexColor.TrimStart('#');

            int red = Convert.ToInt32(hexDigits.Substring(0, 2), 16);
            int green = Convert.ToInt32(hexDigits.Substring(2, 2), 16);
            int blue = Convert.ToInt32(hexDigits.Substring(4, 2), 16);

            double linearRed = ToLinear(red);
            double linearGreen = ToLinear(green);
            double linearBlue = ToLinear(blue);

            double luminance = 0.2126 * linearRed + 0.7152 * linearGreen + 0.0722 * linearBlue;
            return luminance;
        }

        // Rechnet einen Farbanteil (0–255) in echte Lichtstärke (0–1) um
        private double ToLinear(int colorChannel)
        {
            double channelRatio = colorChannel / 255.0;

            if (channelRatio <= 0.04045)
            {
                return channelRatio / 12.92;
            }

            double linearValue = Math.Pow((channelRatio + 0.055) / 1.055, 2.4);
            return linearValue;
        }

        // Wie stark sich zwei Helligkeiten voneinander abheben, von 1 bis 21
        private double GetContrastRatio(double firstLuminance, double secondLuminance)
        {
            double lighter = Math.Max(firstLuminance, secondLuminance);
            double darker = Math.Min(firstLuminance, secondLuminance);

            double contrastRatio = (lighter + 0.05) / (darker + 0.05);
            return contrastRatio;
        }
        #endregion



        #endregion

    }
}