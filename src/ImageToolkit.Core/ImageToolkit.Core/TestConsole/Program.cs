using ImageToolkit.Core.Services;
using System.Drawing;

class Program
{
    static void Main()
    {
        var svc = new ImageService();
        using var fs = File.OpenRead("sample.avif"); // or sample.heic / sample.webp
        try
        {
            using var img = svc.Load(fs, "sample.avif");
            using var thumb = svc.Resize(img, 300, 300);
            using var outFs = File.Create("thumb.jpg");
            svc.Save(thumb, outFs, "thumb.jpg", 85);
            Console.WriteLine("Saved thumb.jpg");
        }
        catch (PlatformNotSupportedException pex)
        {
            Console.WriteLine("Platform not supported: " + pex.Message);
        }
        catch (NotSupportedException nsex)
        {
            Console.WriteLine("Codec missing or unsupported format: " + nsex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Decode failed: " + ex);
        }
    }
}
