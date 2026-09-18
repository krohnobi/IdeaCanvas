using IdeaCanvas.Models;

namespace IdeaCanvas.Interfaces
{
    public interface IMindMapStorageService
    {
        MindMap CreateNap(string title);
        Task DeleteAsync(Guid mapId);
        Task<bool> SaveAsync(MindMap map, string path);
        Task<MindMap?> LoadAsync(string path);
        Task<List<MindMap>> LoadAllAsync();  // für eine Übersichtsseite "meine Mindmaps"
        
    }
}
