using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnrealLib.Interfaces;

namespace UnrealLib.Experimental.FBX;

public enum PropertyType : byte
{
    Bool = (byte)'C',
    Bools = (byte)'b',

    Short = (byte)'Y',

    Int = (byte)'I',
    Ints = (byte)'i',

    Float = (byte)'F',
    Floats = (byte)'f',

    Long = (byte)'L',
    Longs = (byte)'l',

    Double = (byte)'D',
    Doubles = (byte)'d',

    String = (byte)'S',
    Bytes = (byte)'R',
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public record struct FbxPropertyValue : ISerializable
{
    [FieldOffset(0)] public PropertyType Type;

    [FieldOffset(16)] public byte[] Bytes;

    [FieldOffset(1)] public bool Bool;
    [FieldOffset(16)] public bool[] Bools;

    [FieldOffset(1)] public short Short;

    [FieldOffset(1)] public int Int;
    [FieldOffset(16)] public int[] Ints;

    [FieldOffset(1)] public float Float;
    [FieldOffset(16)] public float[] Floats;

    [FieldOffset(1)] public long Long;
    [FieldOffset(16)] public long[] Longs;

    [FieldOffset(1)] public double Double;
    [FieldOffset(16)] public double[] Doubles;

    [FieldOffset(16)] public string String;

    // BYTE
    public FbxPropertyValue(byte[] value) { Type = PropertyType.Bytes; Bytes = value; }

    // BOOL
    public FbxPropertyValue(bool value) { Type = PropertyType.Bool; Bool = value; }
    public FbxPropertyValue(bool[] value) { Type = PropertyType.Bools; Bools = value; }

    // SHORT
    public FbxPropertyValue(short value) { Type = PropertyType.Short; Short = value; }

    // INT
    public FbxPropertyValue(int value) { Type = PropertyType.Int; Int = value; }
    public FbxPropertyValue(int[] value) { Type = PropertyType.Ints; Ints = value; }

    // FLOAT

    public FbxPropertyValue(float value) { Type = PropertyType.Float; Float = value; }
    public FbxPropertyValue(float[] value) { Type = PropertyType.Floats; Floats = value; }

    // LONG
    public FbxPropertyValue(long value) { Type = PropertyType.Long; Long = value; }
    public FbxPropertyValue(long[] value) { Type = PropertyType.Longs; Longs = value; }

    // DOUBLE
    public FbxPropertyValue(double value) { Type = PropertyType.Double; Double = value; }
    public FbxPropertyValue(double[] value) { Type = PropertyType.Doubles; Doubles = value; }

    // STRING
    public FbxPropertyValue(string value) { Type = PropertyType.String; String = value; }

#if DEBUG
    public override readonly string ToString() => Type switch
    {
        PropertyType.Bool => Bool.ToString(),
        PropertyType.Short => Short.ToString(),
        PropertyType.Int => Int.ToString(),
        PropertyType.Float => Float.ToString(),
        PropertyType.Long => Long.ToString(),
        PropertyType.Double => Double.ToString(),
        PropertyType.String => $"\"{String}\"",
        _ => $"{Type}[] ({Bytes.Length} bytes)"
    };
#endif

    public void Serialize(UnrealArchive Ar)
    {
        Debug.Assert(Ar.IsLoading || Type != 0);
        Ar.Serialize(ref Type);

        switch (Type)
        {
            case PropertyType.Bool: Ar.Serialize(ref Bool); break;
            case PropertyType.Bools: Ar.SerializeFbxArray(ref Bools); break;

            case PropertyType.Bytes: Ar.Serialize(ref Bytes); break;

            case PropertyType.Short: Ar.Serialize(ref Short); break;

            case PropertyType.Int: Ar.Serialize(ref Int); break;
            case PropertyType.Ints: Ar.SerializeFbxArray(ref Ints); break;

            case PropertyType.Float: Ar.Serialize(ref Float); break;
            case PropertyType.Floats: Ar.SerializeFbxArray(ref Floats); break;

            case PropertyType.Long: Ar.Serialize(ref Long); break;
            case PropertyType.Longs: Ar.SerializeFbxArray(ref Longs); break;

            case PropertyType.Double: Ar.Serialize(ref Double); break;
            case PropertyType.Doubles: Ar.SerializeFbxArray(ref Doubles); break;

            case PropertyType.String:
                int length = String is null ? 0 : Encoding.UTF8.GetByteCount(String);
                Ar.Serialize(ref length);

                Ar.SerializeFbxString(ref String, length); break;

#if DEBUG
            // This can only happen if we've screwed up somewhere
            default: throw new Exception($"{Type} is not supported!");
#endif
        }
    }
}
