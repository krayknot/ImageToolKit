```markdown
# 🧠 ImageToolkit — WIC-Based Image Framework for .NET

## 1. System Overview

### Purpose

`ImageToolkit` is a modular, self-contained image processing framework in .NET that can:

- Resize, compress, and convert image formats.
- Generate thumbnails.
- Decode and encode multiple image types without external NuGet dependencies.

It supports **both legacy formats (JPG, PNG, BMP, GIF, TIFF)** and **modern formats (WebP, AVIF, HEIC, JXR, JP2)** through **Windows Imaging Component (WIC)** — the native imaging framework built into Windows.

---

## 2. Core Architecture

### Layers

```

Application (Razor / WinForms / Console)
↓
ImageService
↓
IImageCodec abstraction
↓
┌───────────────────────────┬─────────────────────────────┬────────────────────────────┐
│ BuiltInCodec (System.Drawing) │ WicCodec (Windows Imaging Component) │ Custom future codecs │
└───────────────────────────┴─────────────────────────────┴────────────────────────────┘

````

### Components

#### `ImageService`

- Central façade class.
- Selects appropriate codec based on file extension or stream probing.
- Provides resize and save methods using `System.Drawing`.
- Shields consumers from codec complexity.

#### `IImageCodec` Interface

Defines a standard contract for codecs:

- `CanDecode(string pathOrMime)`
- `Decode(Stream input)`
- `Encode(Image image, Stream output, long quality)`
- `SupportedExtensions`

This makes each codec plug-and-play.

#### `BuiltInCodec`

- Uses the built-in GDI+ (`System.Drawing`) pipeline.
- Supports: `.jpg`, `.jpeg`, `.png`, `.bmp`, `.gif`, `.tiff`, `.ico`.
- Uses `ImageCodecInfo` and `EncoderParameters` for compression.

#### `WicCodec`

- Uses Windows Imaging Component (WIC) COM APIs through manual interop (`IWICImagingFactory`, `IWICBitmapDecoder`, `IWICBitmapFrameDecode`, etc.).
- Supports any format that Windows has a registered WIC codec for, including:
  - `.webp` (via Google/Windows WebP codec)
  - `.avif` (via AV1 WIC codec on Windows 11+)
  - `.heic` / `.heif` (requires HEIF Image Extensions)
  - `.jxr` (JPEG XR)
  - `.jp2` (JPEG 2000)
- Produces a 32-bit BGRA buffer which is converted into a `System.Drawing.Bitmap` for compatibility.

#### `WicNative`

- Defines the COM interface mappings for WIC (e.g. `IWICImagingFactory`, `IWICBitmapDecoder`).
- Uses `[ComImport]`, `[Guid]`, and `[InterfaceType]` attributes to call native WIC functions.
- Handles COM object creation via `Activator.CreateInstance(Type.GetTypeFromCLSID(...))`.

---

## 3. How WIC Decoding Works (Step-by-Step)

### 1️⃣ COM Factory Creation

```csharp
IWICImagingFactory factory = WicFactory.CreateFactory();
````

Creates the native WIC Imaging Factory (`CLSID_WICImagingFactory`) — responsible for decoders, encoders, and converters.

### 2️⃣ Create Decoder

```csharp
factory.CreateDecoderFromStream(iStream, IntPtr.Zero, 0, out IWICBitmapDecoder decoder);
```

WIC automatically chooses the correct codec based on image headers, not file extensions.

### 3️⃣ Access Frame

```csharp
decoder.GetFrame(0, out IWICBitmapFrameDecode frame);
```

Most images have one frame; multi-frame formats (GIF, HEIC burst) can expose more.

### 4️⃣ Convert Pixel Format

```csharp
factory.CreateFormatConverter(out IWICFormatConverter converter);
converter.Initialize(frame, GUID_WICPixelFormat32bppBGRA, ...);
```

Standardizes all pixel formats (RGB, YUV, 10-bit HDR, etc.) to 32-bit BGRA, matching `System.Drawing.Bitmap`.

### 5️⃣ Copy Raw Pixels

```csharp
converter.CopyPixels(IntPtr.Zero, stride, bufferSize, bufferPtr);
```

Pixels are copied from native decoder to managed memory.
Stride = width × bytesPerPixel = width × 4 for BGRA.

### 6️⃣ Create Bitmap

```csharp
var bmp = new Bitmap(width, height, stride, PixelFormat.Format32bppArgb, bufferPtr);
```

Wraps the unmanaged pixel buffer into a .NET Bitmap.

### 7️⃣ Clone Bitmap (Optional)

Because the Bitmap references unmanaged memory, it is cloned into a managed buffer for safety.

---

## 4. How Encoding Works

* **BuiltInCodec.Encode** — uses `System.Drawing.Image.Save` with encoder parameters for quality control.
* **WicCodec.Encode** — currently falls back to PNG (lossless) using `System.Drawing`.
* True WIC encoding (WebP/AVIF/HEIC) requires `CreateEncoder` and `CreateNewFrame` COM calls.

---

## 5. Limitations

### ⚙️ Platform Limitations

| Constraint                                       | Details                                                                                           |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------------- |
| **Windows-only**                                 | WIC exists only on Windows. No Linux/macOS support without re-implementation using native codecs. |
| **Requires installed codecs**                    | If the OS lacks a decoder (e.g., WebP/HEIC on older Windows), decoding will fail.                 |
| **System.Drawing is Windows-only (from .NET 6)** | On non-Windows platforms, `System.Drawing.Common` is not supported.                               |
| **No GPU acceleration**                          | WIC runs purely on CPU; decoding large AVIF/HEIC images can be slow.                              |

---

### 🧠 Functional Limitations

| Area                        | Limitation                                                                                                    |
| --------------------------- | ------------------------------------------------------------------------------------------------------------- |
| **Encoding modern formats** | `WicCodec.Encode` currently saves as PNG/JPEG fallback. True WebP/AVIF/HEIC encoding requires extra COM work. |
| **Performance**             | CopyPixels → Bitmap conversion duplicates memory.                                                             |
| **Memory usage**            | Large images allocate large unmanaged buffers; no streaming decode yet.                                       |
| **Multi-frame images**      | Only first frame is decoded.                                                                                  |
| **Color management**        | Ignores ICC/HDR; always converts to 8-bit BGRA.                                                               |
| **Metadata**                | EXIF/orientation not preserved.                                                                               |
| **Thread safety**           | COM WIC objects are not thread-safe. Use one `ImageService` per thread.                                       |

---

### 🧩 Development / Maintenance Limitations

| Aspect                          | Limitation                                                                             |
| ------------------------------- | -------------------------------------------------------------------------------------- |
| **Interop fragility**           | COM signatures are sensitive to incorrect GUIDs, parameter types, or call conventions. |
| **No built-in IStream wrapper** | .NET does not expose `Stream` as COM `IStream`; a custom wrapper must be implemented.  |
| **Error handling**              | WIC COM methods return HRESULTs; you must catch interop exceptions.                    |
| **Debugging**                   | Requires `oleview.exe` or COM inspection to validate GUIDs and interfaces.             |

---

## 6. Strengths

| Feature                     | Benefit                                                                               |
| --------------------------- | ------------------------------------------------------------------------------------- |
| **Zero dependencies**       | No NuGet libraries. Uses only .NET + native Windows APIs.                             |
| **Extensible codec design** | Add your own codec by implementing `IImageCodec`.                                     |
| **Automatic WIC detection** | Chooses decoder by file signature, not extension.                                     |
| **Unified API**             | `ImageService` provides resize, convert, and save methods through a single interface. |
| **Interop ready**           | Output always `System.Drawing.Bitmap` — compatible with any .NET graphics API.        |

---

## 7. How to Extend It

| Extension                  | How                                                                                               |
| -------------------------- | ------------------------------------------------------------------------------------------------- |
| **True WIC encoding**      | Implement `IWICBitmapEncoder` + `IWICBitmapFrameEncode`, then call `Initialize()` and `Commit()`. |
| **Metadata preservation**  | Add `IWICMetadataQueryReader` / `IWICMetadataQueryWriter`.                                        |
| **Async streaming decode** | Implement chunked `IStream.Read` for large files.                                                 |
| **Cross-platform**         | Replace WIC with ImageMagick or SkiaSharp-based codec.                                            |
| **Color space handling**   | Extract ICC profiles and convert to sRGB.                                                         |
| **Animation support**      | Decode multiple frames (`GetFrameCount > 1`).                                                     |

---

## 8. Example Flow Diagram

```
             +---------------------+
             |  File Upload / Read |
             +----------+----------+
                        |
                        v
              +--------------------+
              |   ImageService     |
              |  Detects Format    |
              +---------+----------+
                        |
       +----------------+----------------+
       |                                 |
       v                                 v
+---------------+             +------------------+
| BuiltInCodec  |             |   WicCodec       |
| (GDI+ decode) |             | (WIC COM decode) |
+-------+-------+             +---------+--------+
        |                               |
        v                               v
   System.Drawing.Bitmap <--- pixel copy BGRA ---
        |
        v
   Resize / Compress / Convert
        |
        v
    Save (BuiltInCodec.Encode)
```

---

## 9. Summary

| Category              | Summary                                                                                              |
| --------------------- | ---------------------------------------------------------------------------------------------------- |
| **Core Principle**    | Uses native Windows Imaging Component (WIC) to support modern formats without external dependencies. |
| **Architecture**      | Codec-driven modular design; `ImageService` routes decoding to correct codec.                        |
| **Supported Formats** | BMP, JPG, PNG, GIF, TIFF, ICO (GDI+) + WebP, AVIF, HEIC, JXR, JP2 (via WIC).                         |
| **Cross-Platform**    | Windows-only.                                                                                        |
| **Best Use Cases**    | Server-side or desktop .NET apps prioritizing reliability and native performance.                    |
| **Limitations**       | No true encoding for modern formats; depends on installed WIC codecs.                                |

---

### 🚀 Next Steps

* [ ] Add full **WIC encoder interop** for WebP/AVIF output.
* [ ] Implement **metadata extraction** and **orientation correction**.
* [ ] Create **memory-safe IStream wrapper** for managed <-> COM stream bridging.

```
```
