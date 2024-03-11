using System;
using System.Runtime.InteropServices;

namespace UnrealLib.Core;

/// <summary>
/// A 2x1 of 32-bit floats.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct FVector2
{
    public readonly float X, Y;

    public FVector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public FVector2(float xy)
    {
        X = xy;
        Y = xy;
    }
}

/// <summary>
/// A 2x1 of 16-bit floats.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public readonly record struct FVector2Half
{
    public readonly Half X, Y;

    public FVector2Half(Half x, Half y)
    {
        X = x;
        Y = y;
    }

    public FVector2Half(Half xy)
    {
        X = xy;
        Y = xy;
    }
}

/// <summary>
/// A 3x1 of 32-bit floats.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Vector
{
    public readonly float X, Y, Z;

    public Vector(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector(float xyz)
    {
        X = xyz;
        Y = xyz;
        Z = xyz;
    }

    public static implicit operator Vector(FVector4 value) => new(value.X, value.Y, value.Z);
}

/// <summary>
/// A 3x1 of 16-bit floats.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public readonly record struct FVector3Half
{
    public readonly Half X, Y, Z;

    public FVector3Half(Half x, Half y, Half z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public FVector3Half(Half xyz)
    {
        X = xyz;
        Y = xyz;
        Z = xyz;
    }

    public static implicit operator FVector3Half(Vector value) => new((Half)value.X, (Half)value.Y, (Half)value.Z);
}

/// <summary>
/// A 4x1 of 32-bit floats.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct FVector4
{
    public readonly float X, Y, Z, W;

    public FVector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public FVector4(float xyzw)
    {
        X = xyzw;
        Y = xyzw;
        Z = xyzw;
        W = xyzw;
    }
}

/// <summary>
/// A 4x1 of 16-bit floats.
/// </summary>

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public readonly record struct FVector4Half
{
    public readonly Half X, Y, Z, W;

    public FVector4Half(Half x, Half y, Half z, Half w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public FVector4Half(Half xyzw)
    {
        X = xyzw;
        Y = xyzw;
        Z = xyzw;
        W = xyzw;
    }
}
