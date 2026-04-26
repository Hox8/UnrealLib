// Adapted from https://github.com/torvalds/linux/blob/master/lib/lzo/lzo1x_decompress_safe.c.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace UnrealLib.Core.Compression;

public static partial class CompressionManager
{
    public static bool Decompress_LZO(byte[] compressed, int compressedLength, byte[] uncompressed, int uncompressedLength)
    {
        return Lzo1xDecompressor.Decompress(compressed.AsSpan(0, compressedLength), uncompressed.AsSpan(0, uncompressedLength), out _) == LzoStatus.OK;
    }

    public static void Compress_LZO(FileStream inputStream, FileStream outputStream, int uncompressedLength)
    {
        throw new NotImplementedException();
    }
}

public enum LzoStatus
{
    OK = 0,
    Error = -1,
    InputOverrun = -4,
    OutputOverrun = -5,
    LookbehindOverrun = -6,
    InputNotConsumed = -8,
}

internal static unsafe class Lzo1xDecompressor
{
    private const int M2_MAX_OFFSET = 0x0800;
    private const nuint MIN_ZERO_RUN_LENGTH = 4;
    private const ulong MAX_255_COUNT = (ulong.MaxValue / 255) - 2;     // nuint is apparently not constant, so this is 64-bit only

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HAVE_IP(byte* ip, byte* ip_end, nuint x) => (nuint)(ip_end - ip) >= x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HAVE_OP(byte* op, byte* op_end, nuint x) => (nuint)(op_end - op) >= x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void COPY8(byte* dst, byte* src) => Unsafe.WriteUnaligned(dst, Unsafe.ReadUnaligned<ulong>(src));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void COPY4(byte* dst, byte* src) => Unsafe.WriteUnaligned(dst, Unsafe.ReadUnaligned<uint>(src));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort GetUnalignedLE16(byte* p) => Unsafe.ReadUnaligned<ushort>(p);

    internal static LzoStatus Decompress(ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
    {
        fixed (byte* inPtr = input)
        fixed (byte* outPtr = output)
        {
            nuint outLen = (nuint)output.Length;
            var result = DecompressCore(inPtr, (nuint)input.Length, outPtr, ref outLen);
            bytesWritten = (int)outLen;
            return result;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static LzoStatus DecompressCore(byte* @in, nuint in_len, byte* @out, ref nuint out_len)
    {
        byte* op = @out;
        byte* ip = @in;
        nuint t = 0;
        nuint next = 0;
        nuint state = 0;
        byte* m_pos = null;
        byte* ip_end = @in + in_len;
        byte* op_end = @out + out_len;
        byte bitstream_version;

        if (in_len < 3)
            goto input_overrun;

        if (in_len >= 5 && *ip == 17)
        {
            bitstream_version = ip[1];
            ip += 2;
        }
        else
        {
            bitstream_version = 0;
        }

        if (*ip > 17)
        {
            t = (nuint)(*ip++ - 17);
            if (t < 4) { next = t; goto match_next; }
            goto copy_literal_run;
        }

        // for(;;) has been replaced with a goto since C# doesn't allow goto'ing to labels in different scopes?
    loop_top:
        t = *ip++;

        if (t < 16)
        {
            if (state == 0)
            {
                if (t == 0)
                {
                    nuint offset;
                    byte* ip_last = ip;

                    while (*ip == 0)
                    {
                        ip++;
                        if (!HAVE_IP(ip, ip_end, 1)) goto input_overrun;
                    }
                    offset = (nuint)(ip - ip_last);
                    if (offset > MAX_255_COUNT) return LzoStatus.Error;

                    offset = (offset << 8) - offset;
                    t += offset + 15 + *ip++;
                }
                t += 3;
                goto copy_literal_run;
            }
            else if (state != 4)
            {
                next = t & 3;
                m_pos = op - 1;
                m_pos -= t >> 2;
                m_pos -= (nuint)(*ip++ << 2);
                if (m_pos < @out) goto lookbehind_overrun;
                if (!HAVE_OP(op, op_end, 2)) goto output_overrun;
                op[0] = m_pos[0];
                op[1] = m_pos[1];
                op += 2;
                goto match_next;
            }
            else
            {
                next = t & 3;
                m_pos = op - (1 + M2_MAX_OFFSET);
                m_pos -= t >> 2;
                m_pos -= (nuint)(*ip++ << 2);
                t = 3;
            }
        }
        else if (t >= 64)
        {
            next = t & 3;
            m_pos = op - 1;
            m_pos -= (t >> 2) & 7;
            m_pos -= (nuint)(*ip++ << 3);
            t = (t >> 5) - 1 + (3 - 1);
        }
        else if (t >= 32)
        {
            t = (t & 31) + (3 - 1);
            if (t == 2)
            {
                nuint offset;
                byte* ip_last = ip;

                while (*ip == 0)
                {
                    ip++;
                    if (!HAVE_IP(ip, ip_end, 1)) goto input_overrun;
                }
                offset = (nuint)(ip - ip_last);
                if (offset > MAX_255_COUNT) return LzoStatus.Error;

                offset = (offset << 8) - offset;
                t += offset + 31 + *ip++;
                if (!HAVE_IP(ip, ip_end, 2)) goto input_overrun;
            }
            m_pos = op - 1;
            next = GetUnalignedLE16(ip);
            ip += 2;
            m_pos -= next >> 2;
            next &= 3;
        }
        else
        {
            if (!HAVE_IP(ip, ip_end, 2)) goto input_overrun;
            next = GetUnalignedLE16(ip);

            if (((next & 0xfffcu) == 0xfffcu)
                && ((t & 0xf8u) == 0x18u)
                && bitstream_version != 0)
            {
                if (!HAVE_IP(ip, ip_end, 3)) goto input_overrun;
                t &= 7;
                t |= (nuint)(ip[2] << 3);
                t += MIN_ZERO_RUN_LENGTH;
                if (!HAVE_OP(op, op_end, t)) goto output_overrun;
                Unsafe.InitBlock(op, 0, (uint)t);
                op += t;
                next &= 3;
                ip += 3;
                goto match_next;
            }
            else
            {
                m_pos = op;
                m_pos -= (nuint)((t & 8u) << 11);
                t = (t & 7) + (3 - 1);
                if (t == 2)
                {
                    nuint offset;
                    byte* ip_last = ip;

                    while (*ip == 0)
                    {
                        ip++;
                        if (!HAVE_IP(ip, ip_end, 1)) goto input_overrun;
                    }
                    offset = (nuint)(ip - ip_last);
                    if (offset > MAX_255_COUNT) return LzoStatus.Error;

                    offset = (offset << 8) - offset;
                    t += offset + 7 + *ip++;
                    if (!HAVE_IP(ip, ip_end, 2)) goto input_overrun;
                    next = GetUnalignedLE16(ip);
                }
                ip += 2;
                m_pos -= next >> 2;
                next &= 3;
                if (m_pos == op) goto eof_found;
                m_pos -= 0x4000;
            }
        }

        if (m_pos < @out) goto lookbehind_overrun;

        if ((nuint)(op - m_pos) >= 8)
        {
            byte* oe = op + t;
            if (HAVE_OP(op, op_end, t + 15))
            {
                do
                {
                    COPY8(op, m_pos); op += 8; m_pos += 8;
                    COPY8(op, m_pos); op += 8; m_pos += 8;
                } while (op < oe);
                op = oe;
                if (HAVE_IP(ip, ip_end, 6))
                {
                    state = next;
                    COPY4(op, ip);
                    op += next;
                    ip += next;
                    goto loop_top;
                }
            }
            else
            {
                if (!HAVE_OP(op, op_end, t)) goto output_overrun;
                do { *op++ = *m_pos++; } while (op < oe);
            }
        }
        else
        {
            byte* oe = op + t;
            if (!HAVE_OP(op, op_end, t)) goto output_overrun;
            op[0] = m_pos[0];
            op[1] = m_pos[1];
            op += 2;
            m_pos += 2;
            do { *op++ = *m_pos++; } while (op < oe);
        }

    match_next:
        state = next;
        t = next;
        if (HAVE_IP(ip, ip_end, 6) && HAVE_OP(op, op_end, 4))
        {
            COPY4(op, ip);
            op += t;
            ip += t;
        }
        else
        {
            if (!HAVE_IP(ip, ip_end, t + 3)) goto input_overrun;
            if (!HAVE_OP(op, op_end, t)) goto output_overrun;
            while (t > 0) { *op++ = *ip++; t--; }
        }
        goto loop_top;

        // Hoisted out so we can goto it from earlier in the loop
    copy_literal_run:
        {
            byte* ie = ip + t;
            byte* oe = op + t;
            if (HAVE_IP(ip, ip_end, t + 15) && HAVE_OP(op, op_end, t + 15))
            {
                do
                {
                    COPY8(op, ip); op += 8; ip += 8;
                    COPY8(op, ip); op += 8; ip += 8;
                } while (ip < ie);
                ip = ie;
                op = oe;
            }
            else
            {
                if (!HAVE_OP(op, op_end, t)) goto output_overrun;
                if (!HAVE_IP(ip, ip_end, t + 3)) goto input_overrun;
                do { *op++ = *ip++; } while (--t > 0);
            }
        }
        state = 4;
        goto loop_top;

    eof_found:
        out_len = (nuint)(op - @out);
        return t != 3 ? LzoStatus.Error :
               ip == ip_end ? LzoStatus.OK :
               ip < ip_end ? LzoStatus.InputNotConsumed :
                              LzoStatus.InputOverrun;

    input_overrun:
        out_len = (nuint)(op - @out);
        return LzoStatus.InputOverrun;

    output_overrun:
        out_len = (nuint)(op - @out);
        return LzoStatus.OutputOverrun;

    lookbehind_overrun:
        out_len = (nuint)(op - @out);
        return LzoStatus.LookbehindOverrun;
    }
}
