using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;

namespace UnrealLib
{
    public static class StreamExtensions
    {
        /// <summary>
        /// CopyTo(), but with a specified length. Essentially a Write(Stream, Stream)
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="length"></param>
        public static void ConstrainedCopy(this Stream src, Stream dest, int length, int bufferSize)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            int bytesRead;
            while (length > 0)
            {
                bytesRead = src.Read(buffer, 0, Math.Min(length, buffer.Length));
                length -= bytesRead;

                dest.Write(buffer, 0, bytesRead);
            }

            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
