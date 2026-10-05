using IdeaCanvas.Interfaces;
using IdeaCanvas.Models;
using SkiaSharp;
using System.Text.Json;


namespace IdeaCanvas.Services
{
    internal class MindMapStorageService : IMindMapStorageService
    {
        public MindMap CreateMap(string title)
        {
            return new MindMap { Title = title };
        }

        public async Task<bool> DeleteAsync(Guid mapId)
        {
            string path = GetFilePath(mapId);
            if (!File.Exists(path))
            {
                return Task.FromResult(false);
            }
            
            File.Delete(path);
            return Task.FromResult(true);
        }

        public async Task<List<MindMap>> LoadAllAsync()
        {
            throw new NotImplementedException();
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

<<<<<<< HEAD
        public async Task<bool> SaveAsync(MindMap map)
=======
        public string SerializeMap(MindMap map)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(map, options);
        }

        [Obsolete]
        public async Task<bool> SaveAsync(MindMap map, string path)
>>>>>>> d048e40b61560ad89f30db1416217ca0dd0bd66f
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

<<<<<<< HEAD
        private string GetFilePath(Guid mapId)
        {
            string mapsDirectory = Path.Combine(FileSystem.AppDataDirectory, "MindMaps");
            string fileName = $"{mapId}.json";

            return Path.Combine(mapsDirectory, fileName);
        }

=======
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

            document.Close(); // <- HIER passiert das eigentliche Schreiben in pdfStream

            return true;
        }
>>>>>>> d048e40b61560ad89f30db1416217ca0dd0bd66f
    }
}
