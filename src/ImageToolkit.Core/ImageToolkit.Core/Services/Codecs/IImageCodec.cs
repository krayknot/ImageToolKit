using System.Drawing;

namespace ImageToolkit.Core.Services.Codecs
{
    public interface IImageCodec
    {
        string[] SupportedExtensions { get; }
        bool CanDecode(string pathOrMime);
        Image Decode(System.IO.Stream input);
        void Encode(Image image, System.IO.Stream output, long quality);
    }
}
