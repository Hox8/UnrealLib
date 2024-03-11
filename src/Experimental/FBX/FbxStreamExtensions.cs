using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace UnrealLib.Experimental.FBX;

public static class FbxStreamExtensions
{
    public static unsafe void SerializeFbxString(this UnrealArchive Ar, ref string value, int length)
    {
        if (length == 0) return;

        if (Ar.IsLoading)
        {
            Span<byte> buffer = stackalloc byte[length];
            Ar.ReadExactly(buffer);

            value = Encoding.UTF8.GetString(buffer);

            // @TODO check for and re-format binary separator '\0\x1' into "::"? What and why is this???
        }
        else
        {
            Ar.Write(Encoding.UTF8.GetBytes(value));

            // @TODO would need to do opposite of above todo when I implement it
        }
    }

    public static unsafe void SerializeFbxArray<T>(this UnrealArchive Ar, ref T[] value) where T : unmanaged
    {
        int length = Ar.IsLoading ? default : value.Length;
        int encoding = 1;
        int compressedLength = length * sizeof(T);

        Ar.Serialize(ref length);               // Number of elements in T[]
        Ar.Serialize(ref encoding);             // 1 if data is zlib-compressed, 0 if uncompressed
        Ar.Serialize(ref compressedLength);     // Length of T[] in bytes. Used by both compressed and uncompressed data

        // ZLib-compressed
        if (encoding == 1)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(compressedLength);
            byte[] result = ArrayPool<byte>.Shared.Rent(length * sizeof(T));

            using var bufferStream = new MemoryStream(buffer, 0, compressedLength);
            using var resultStream = new MemoryStream(result);

            // @TODO see if some of !Loading and Loading logic can be merged. Stream switching surely can -- but would it hurt readability?
            if (!Ar.IsLoading)
            {
                int offsetStart = (int)Ar.Position;

                // Hack -- switch underlying stream temporarily to serialize value into buffer

                var origStream = Ar._buffer;
                Ar._buffer = bufferStream;
                Ar.Serialize(ref value, length);
                Ar._buffer = origStream;
                bufferStream.Position = 0;

                // Deflate and write uncompressed data in buffer to output stream
                // I am confident these unmanaged types will always have a positive
                // compression ratio (unless very small length), so no checks in that regard
                using (var deflator = new ZLibStream(Ar._buffer, CompressionLevel.SmallestSize, true))
                {
                    bufferStream.ConstrainedCopy(deflator, compressedLength, 8192);
                }

                compressedLength = (int)Ar.Position - offsetStart;

                // Update compressedLength
                Ar.Position = offsetStart - sizeof(int);
                Ar.Serialize(ref compressedLength);
                Ar.Position += compressedLength;
            }
            else
            {
                Ar.ReadExactly(buffer, 0, compressedLength);
                new ZLibStream(bufferStream, CompressionMode.Decompress).CopyTo(resultStream);

                // Hack -- switch underlying stream temporarily so we can serialize the now-inflated data into value

                var origStream = Ar._buffer;

                Ar._buffer = resultStream;
                resultStream.Position = 0;

                Ar.Serialize(ref value, length);
                Ar._buffer = origStream;
            }

            ArrayPool<byte>.Shared.Return(buffer);
            ArrayPool<byte>.Shared.Return(result);
        }
        else
        {
            Debug.Assert(encoding == 0, "Encoding should only ever be 0 or 1.");

            Ar.Serialize(ref value, length);
        }
    }
}
