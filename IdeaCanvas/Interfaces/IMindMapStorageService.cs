using IdeaCanvas.Models;

namespace IdeaCanvas.Interfaces
{
    public interface IMindMapStorageService
    {
        MindMap CreateMap(string title);
        Task<bool DeleteAsync(Guid mapId);
        Task<bool> SaveAsync(MindMap map);
        Task<MindMap?> LoadAsync(Guid mapId);
        Task<List<MindMap>> LoadAllAsync();  // für eine Übersichtsseite "meine Mindmaps"

    }
}
