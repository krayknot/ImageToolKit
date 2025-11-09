using System;
using System.Runtime.InteropServices;

namespace ImageToolkit.Core.Native
{
    // CLSID for WICImagingFactory (commonly available)
    internal static class WicGuids
    {
        public static readonly Guid CLSID_WICImagingFactory = new Guid("CACAF262-9370-4615-A13B-9F5539DA4C0A");
        public static readonly Guid IID_IWICImagingFactory = new Guid("EC5EC8A9-C395-4314-9C77-54D7A935FF70");
        public static readonly Guid IID_IWICBitmapDecoder = new Guid("9EDDE9E7-8DEE-47ea-99DF-E6FAF2ED44BF");
        public static readonly Guid IID_IWICBitmapFrameDecode = new Guid("3B16811B-6A43-4ec9-B713-3D930C13B940");
        public static readonly Guid IID_IWICFormatConverter = new Guid("00000301-A8F2-4877-B8B5-FFB3A92D3496");
    }

    [ComImport, Guid("EC5EC8A9-C395-4314-9C77-54D7A935FF70"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IWICImagingFactory
    {
        // We declare only the methods we need. Real interface has many more.
        void CreateDecoderFromStream([MarshalAs(UnmanagedType.IUnknown)] object pIStream, IntPtr pguidVendor,
            uint metadataOptions, out IWICBitmapDecoder ppIDecoder);

        void CreateFormatConverter(out IWICFormatConverter ppIFormatConverter);
        // other methods omitted
    }

    [ComImport, Guid("9EDDE9E7-8DEE-47ea-99DF-E6FAF2ED44BF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IWICBitmapDecoder
    {
        // minimal subset
        void QueryCapability(); // placeholder
        void GetFrameCount(out uint pCount);
        void GetFrame(uint index, out IWICBitmapFrameDecode ppIBitmapFrame);
        // many methods omitted
    }

    [ComImport, Guid("3B16811B-6A43-4ec9-B713-3D930C13B940"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IWICBitmapFrameDecode
    {
        void GetSize(out uint pWidth, out uint pHeight);
        void CopyPixels(IntPtr prc, uint cbStride, uint cbBufferSize, IntPtr pbBuffer);
        // many methods omitted
    }

    [ComImport, Guid("00000301-A8F2-4877-B8B5-FFB3A92D3496"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IWICFormatConverter
    {
        void Initialize(IWICBitmapFrameDecode pISource, Guid dstFormat, uint dither, IntPtr pIPalette, double alphaThresholdPercent, uint paletteTranslate);
        void CopyPixels(IntPtr prc, uint cbStride, uint cbBufferSize, IntPtr pbBuffer);
    }

    internal static class WicFactory
    {
        public static IWICImagingFactory CreateFactory()
        {
            // Create COM instance of WICImagingFactory
            var type = Type.GetTypeFromCLSID(WicGuids.CLSID_WICImagingFactory, throwOnError: false);
            if (type == null) throw new PlatformNotSupportedException("WIC Imaging Factory not available on this OS.");
            var obj = Activator.CreateInstance(type);
            return (IWICImagingFactory)obj;
        }
    }
}
