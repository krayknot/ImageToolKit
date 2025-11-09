using System.Drawing;
using System.Drawing.Imaging;

namespace ImageToolkit.Core.Services.Codecs
{
    public class BuiltInCodec : IImageCodec
    {
        public string[] SupportedExtensions => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico" };

        public bool CanDecode(string pathOrMime)
        {
            var ext = Path.GetExtension(pathOrMime ?? "").ToLowerInvariant();
            return SupportedExtensions.Contains(ext);
        }

        public Image Decode(Stream input) => Image.FromStream(input);

        public void Encode(Image image, Stream output, long quality)
        {
            var encoder = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(e => e.FormatID == ImageFormat.Jpeg.Guid)
                ?? ImageCodecInfo.GetImageEncoders().First();
            var encParams = new EncoderParameters(1);
            encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            image.Save(output, encoder, encParams);
        }
    }
}
