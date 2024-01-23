namespace UnrealLib.Core;

public record struct Vector2
{
    public float X, Y;

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public Vector2(float xy)
    {
        X = xy;
        Y = xy;
    }
}

public record struct Vector3
{
    public float X, Y, Z;

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3(float xyz)
    {
        X = xyz;
        Y = xyz;
        Z = xyz;
    }
}

public record struct Vector4
{
    public float X, Y, Z, W;

    public Vector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public Vector4(float xyzw)
    {
        X = xyzw;
        Y = xyzw;
        Z = xyzw;
        W = xyzw;
    }
}
