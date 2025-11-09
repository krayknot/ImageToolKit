using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using ImageToolkit.Core.Native;

namespace ImageToolkit.Core.Services.Codecs
{
    public class WicCodec : IImageCodec
    {
        // WIC supports many extensions; we claim these and defer to factory to actually decode.
        public string[] SupportedExtensions => new[] { ".webp", ".avif", ".heic", ".jxr", ".jp2" };

        public bool CanDecode(string pathOrMime)
        {
            if (string.IsNullOrEmpty(pathOrMime)) return false;
            var ext = Path.GetExtension(pathOrMime).ToLowerInvariant();
            return SupportedExtensions.Contains(ext);
        }

        public Image Decode(Stream input)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("WIC codec works only on Windows.");

            // Copy to memory stream to ensure seekable stream
            using var ms = new MemoryStream();
            input.CopyTo(ms);
            ms.Position = 0;

            // Create a COM IStream wrapper from the managed stream
            var comStream = new ImageToolkit.Core.Native.ManagedIStream(ms);

            var factory = WicFactory.CreateFactory();
            // The real WIC method CreateDecoderFromStream expects an IStream.
            factory.CreateDecoderFromStream(comStream, IntPtr.Zero, 0, out var decoder);

            decoder.GetFrameCount(out var count);
            decoder.GetFrame(0, out var frame);

            frame.GetSize(out var w, out var h);

            // Create format converter and convert to 32bpp BGRA
            factory.CreateFormatConverter(out var converter);

            // GUID for 32bpp BGRA in WIC: GUID_WICPixelFormat32bppBGRA = {6fddc324-4e03-4bfe-b185-3d77768dc900}
            var guid32bppBGRA = new Guid("6FDDC324-4E03-4BFE-B185-3D77768DC900");
            converter.Initialize(frame, guid32bppBGRA, 0, IntPtr.Zero, 0.0, 0);

            // prepare buffer and copy pixels
            int stride = (int)(w * 4);
            int bufferSize = stride * (int)h;
            var buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                converter.CopyPixels(IntPtr.Zero, (uint)stride, (uint)bufferSize, buffer);

                // create Bitmap from BGRA -> need to convert to BGR(A) accepted by System.Drawing (BGRA works).
                var bmp = new Bitmap((int)w, (int)h, stride, PixelFormat.Format32bppArgb, buffer);

                // NOTE: because bitmap uses unmanaged buffer, clone to managed bitmap to free buffer.
                var clone = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(clone))
                    g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);

                return clone;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void Encode(Image image, Stream output, long quality)
        {
            // WIC encoders require a full COM encoder pipeline.
            // For simplicity, fallback to PNG/JPEG via System.Drawing here.
            var ext = Path.GetExtension("dummy.png");
            var encoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(e => e.FormatID == ImageFormat.Png.Guid);
            image.Save(output, encoder, null);
        }
    }
}
