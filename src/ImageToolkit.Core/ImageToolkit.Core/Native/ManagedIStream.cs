using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ImageToolkit.Core.Native
{
    /// <summary>
    /// Wraps a managed Stream to expose it as a COM IStream for WIC interop.
    /// </summary>
    internal sealed class ManagedIStream : IStream
    {
        private readonly Stream _stream;

        public ManagedIStream(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public void Read(byte[] buffer, int cb, IntPtr pcbRead)
        {
            int read = _stream.Read(buffer, 0, cb);
            if (pcbRead != IntPtr.Zero)
                Marshal.WriteInt32(pcbRead, read);
        }

        public void Write(byte[] buffer, int cb, IntPtr pcbWritten)
        {
            _stream.Write(buffer, 0, cb);
            if (pcbWritten != IntPtr.Zero)
                Marshal.WriteInt32(pcbWritten, cb);
        }

        public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
        {
            SeekOrigin origin = dwOrigin switch
            {
                0 => SeekOrigin.Begin,
                1 => SeekOrigin.Current,
                2 => SeekOrigin.End,
                _ => SeekOrigin.Begin
            };
            long pos = _stream.Seek(dlibMove, origin);
            if (plibNewPosition != IntPtr.Zero)
                Marshal.WriteInt64(plibNewPosition, pos);
        }

        public void SetSize(long libNewSize)
        {
            _stream.SetLength(libNewSize);
        }

        public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            const int bufSize = 4096;
            byte[] buffer = new byte[bufSize];
            long remaining = cb;
            int totalRead = 0, totalWritten = 0;
            while (remaining > 0)
            {
                int toRead = remaining > bufSize ? bufSize : (int)remaining;
                int read = _stream.Read(buffer, 0, toRead);
                if (read == 0) break;
                totalRead += read;
                pstm.Write(buffer, read, IntPtr.Zero);
                totalWritten += read;
                remaining -= read;
            }
            if (pcbRead != IntPtr.Zero) Marshal.WriteInt32(pcbRead, totalRead);
            if (pcbWritten != IntPtr.Zero) Marshal.WriteInt32(pcbWritten, totalWritten);
        }

        public void Commit(int grfCommitFlags) => _stream.Flush();

        public void Revert() => throw new NotSupportedException();

        public void LockRegion(long libOffset, long cb, int dwLockType) =>
            throw new NotSupportedException();

        public void UnlockRegion(long libOffset, long cb, int dwLockType) =>
            throw new NotSupportedException();

        public void Stat(out STATSTG pstatstg, int grfStatFlag)
        {
            pstatstg = new STATSTG
            {
                cbSize = _stream.CanSeek ? _stream.Length : 0,
                type = 2 // STGTY_STREAM
            };
        }

        public void Clone(out IStream ppstm) =>
            throw new NotSupportedException();
    }
}
