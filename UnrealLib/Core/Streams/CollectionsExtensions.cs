using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnrealLib.Core;

public static class CollectionsExtensions
{
    /// <summary>
    /// Get a <see cref="Span{T}"/> view over a <see cref="List{T}"/>'s data.
    /// Items should not be added or removed from the <see cref="List{T}"/> while the <see cref="Span{T}"/> is in use.
    /// </summary>
    /// <param name="list">The list to get the data view over.</param>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(this List<T>? value) => CollectionsMarshal.AsSpan(value);

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(this List<T> value, int start) => CollectionsMarshal.AsSpan(value).Slice(start);

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(this List<T> value, int start, int length) => CollectionsMarshal.AsSpan(value).Slice(start, length);



    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> AsBytes<T>(this Span<T> value) where T : struct => MemoryMarshal.AsBytes(value);

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> AsBytes(this ReadOnlySpan<char> value) => MemoryMarshal.AsBytes(value);

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> AsBytes<T>(this List<T> value) where T : struct => value.AsSpan().AsBytes();

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> AsBytes<T>(this T[] value) where T : struct => value.AsSpan().AsBytes();

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> AsBytes(this string value) => value.AsSpan().AsBytes();
}
