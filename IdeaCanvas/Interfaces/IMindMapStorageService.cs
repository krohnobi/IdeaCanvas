using IdeaCanvas.Models;

namespace IdeaCanvas.Interfaces
{
    public interface IMindMapStorageService
    {
        MindMap CreateMap(string title);
        Task<bool> DeleteAsync(Guid mapId);
        string SerializeMap(MindMap map);
        Task<bool> SaveAsync(MindMap map);
        Task<MindMap?> LoadAsync(Guid mapId);
        Task<List<MindMapSummary>> LoadAllAsync();
        bool ExportPdf(string pngBase64, string targetPdfPath);
    }
}