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

        public Task DeleteAsync(Guid mapId)
        {
            throw new NotImplementedException();
        }

        public Task<List<MindMap>> LoadAllAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<MindMap?> LoadAsync(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("MindMap file not found.", path);
            }

            // Keine Optionen nötig, da wir das Standard-Format (PascalCase) nutzen
            using FileStream openStream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<MindMap>(openStream);
        }

        public string SerializeMap(MindMap map)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(map, options);
        }

        [Obsolete]
        public async Task<bool> SaveAsync(MindMap map, string path)
        {
            try
            {
                // Nur noch die Formatierung für schöne Lesbarkeit (optional)
                var options = new JsonSerializerOptions { WriteIndented = true };

                // Ordner erstellen, falls er nicht existiert
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using FileStream createStream = File.Create(path);
                await JsonSerializer.SerializeAsync(createStream, map, options);

                return true;
            }
            catch (Exception)
            {
                // Hier ggf. Fehler loggen
                return false;
            }
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

            document.Close(); // <- HIER passiert das eigentliche Schreiben in pdfStream

            return true;
        }
    }
}
