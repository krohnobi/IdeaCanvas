using IdeaCanvas.Interfaces;
using IdeaCanvas.Models;
using SkiaSharp;
using System.IO.Pipelines;
using System.Text.Json;


namespace IdeaCanvas.Services
{
    internal class MindMapStorageService : IMindMapStorageService
    {
        public MindMap CreateMap(string title)
        {
            return new MindMap { Title = title };
        }

        public Task<bool> DeleteAsync(Guid mapId)
        {
            string path = GetFilePath(mapId);
            if (!File.Exists(path))
            {
                return Task.FromResult(false);
            }

            File.Delete(path);
            return Task.FromResult(true);
        }

        public async Task<List<MindMapSummary>> LoadAllAsync()
        {
            string mapsDirectory = GetMapsDirectory();
            if (!Directory.Exists(mapsDirectory))
            {
                return new List<MindMapSummary>();
            }

            string[] files = Directory.GetFiles(mapsDirectory, "*.json");
            List<MindMapSummary> summaries = new();
            foreach (string filePath in files)
            {
                try
                {
                    byte[] content = await File.ReadAllBytesAsync(filePath);
                    var mindMap = JsonSerializer.Deserialize<MindMap>(content);
                    if (mindMap == null)
                    {
                        continue;
                    }
                    summaries.Add(new MindMapSummary { Id = mindMap.Id, Title = mindMap.Title, CreatedAt = mindMap.CreatedAt, ModifiedAt = mindMap.ModifiedAt, NodeCount = mindMap.Nodes.Count });
                }
                catch (Exception) { }
            }
            return summaries.OrderByDescending(item => item.ModifiedAt).ToList();
        }

        public async Task<MindMap?> LoadAsync(Guid mapId)
        {
            string path = GetFilePath(mapId);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("MindMap file not found.", path);
            }

            using FileStream openStream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<MindMap>(openStream);
        }

        public string SerializeMap(MindMap map)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(map, options);
        }

        public async Task<bool> SaveAsync(MindMap map)
        {
            try
            {
                string filePath = GetFilePath(map.Id);

                var options = new JsonSerializerOptions { WriteIndented = true };

                // Ordner erstellen, falls er nicht existiert
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                map.ModifiedAt = DateTime.Now;

                using FileStream createStream = File.Create(filePath);
                await JsonSerializer.SerializeAsync(createStream, map, options);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GetFilePath(Guid mapId)
        {
            string mapsDirectory = GetMapsDirectory()
            string fileName = $"{mapId}.json";

            return Path.Combine(mapsDirectory, fileName);
        }

        private string GetMapsDirectory()
        {
            return Path.Combine(FileSystem.AppDataDirectory, "MindMaps");
        }

        public bool ExportPdf(string pngBase64, string targetPdfPath)
        {
            byte[] pngBytes = Convert.FromBase64String(pngBase64);

            using var pngStream = new MemoryStream(pngBytes);
            using var bitmap = SKBitmap.Decode(pngStream);
            if (bitmap == null) return false;

            using var pdfStream = File.OpenWrite(targetPdfPath);
            using var document = SKDocument.CreatePdf(pdfStream);

            using (var canvas = document.BeginPage(bitmap.Width, bitmap.Height))
            {
                canvas.DrawBitmap(bitmap, new SKPoint(0, 0), SKSamplingOptions.Default);
                document.EndPage();
            }

            document.Close();

            return true;
        }
    }
}