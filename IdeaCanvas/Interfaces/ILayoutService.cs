using IdeaCanvas.Models;

namespace IdeaCanvas.Interfaces
{
    public interface ILayoutService
    {
        void ApplyLayout(MindMap map, double curveStrength);
    }
}