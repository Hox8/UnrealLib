namespace UnrealLib.Core;

public record struct Color
{
    public byte B, G, R, A;

    public Color(byte b, byte g, byte r, byte a)
    {
        B = b;
        G = g;
        R = r;
        A = a;
    }

    public Color(byte bgra)
    {
        B = bgra;
        G = bgra;
        R = bgra;
        A = bgra;
    }
}

public record struct LinearColor
{
    public float R, G, B, A;

    public LinearColor(float rgba)
    {
        B = rgba;
        G = rgba;
        R = rgba;
        A = rgba;
    }

    public LinearColor(float r, float g, float b, float a)
    {
        G = g;
        R = r;
        B = b;
        A = a;
    }
}
