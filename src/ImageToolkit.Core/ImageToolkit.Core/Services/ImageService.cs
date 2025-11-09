using System.Drawing;
using ImageToolkit.Core.Services.Codecs;

namespace ImageToolkit.Core.Services
{
    public class ImageService
    {
        private readonly List<IImageCodec> _codecs;

        public ImageService()
        {
            _codecs = new List<IImageCodec>
            {
                new BuiltInCodec(),
                new WicCodec()
            };
        }

        private IImageCodec Resolve(Stream s, string pathHint)
        {
            // Prefer explicit extension match
            if (!string.IsNullOrEmpty(pathHint))
            {
                foreach (var c in _codecs)
                    if (c.CanDecode(pathHint)) return c;
            }
            // fallback: try codecs by attempting a decode (non-destructive memory copy)
            var ms = new MemoryStream();
            s.Position = 0;
            s.CopyTo(ms);
            ms.Position = 0;
            foreach (var c in _codecs)
            {
                try
                {
                    ms.Position = 0;
                    var img = c.Decode(ms);
                    if (img != null) { img.Dispose(); ms.Position = 0; return c; }
                }
                catch { ms.Position = 0; }
            }
            throw new NotSupportedException("No codec could decode the stream. Install corresponding WIC codec or add a codec class.");
        }

        public Image Load(Stream s, string pathHint = null)
        {
            var codec = Resolve(s, pathHint);
            s.Position = 0;
            return codec.Decode(s);
        }

        public void Save(Image img, Stream outStream, string pathHint, long quality = 85)
        {
            var ext = Path.GetExtension(pathHint ?? "").ToLowerInvariant();
            var codec = _codecs.FirstOrDefault(c => c.SupportedExtensions.Contains(ext)) ?? _codecs.First();
            codec.Encode(img, outStream, quality);
        }

        public Image Resize(Image src, int w, int h)
        {
            var bmp = new Bitmap(w, h);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, 0, 0, w, h);
            return bmp;
        }
    }
}
