using System.Buffers;
using System.Diagnostics;
using System.IO;

namespace UnrealLib.Core;

public static class StreamExtensions
{
    /// <summary>
    /// A version of <see cref="Stream.CopyTo(Stream)"/>, but with a specified length.
    /// </summary>
    public static void ConstrainedCopy(this Stream src, Stream dest, int length, int bufferSize = 81920)
    {
        Debug.Assert(bufferSize > 0);
        Debug.Assert(src.CanRead);
        Debug.Assert(dest.CanWrite);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        int bytesRead;
        while ((bytesRead = src.Read(buffer, 0, System.Math.Min(length, buffer.Length))) != 0)
        {
            dest.Write(buffer, 0, bytesRead);
            length -= bytesRead;
        }

        ArrayPool<byte>.Shared.Return(buffer);

        Debug.Assert(length == 0);
    }
}
