using System.Runtime.InteropServices;

namespace UnrealLib.Core;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct FBoxSphereBounds
{
    public Vector Origin, BoxExtent;
    public float SphereRadius;

    public FBoxSphereBounds(Vector origin, Vector boxExtent, float sphereRadius)
    {
        Origin = origin;
        BoxExtent = boxExtent;
        SphereRadius = sphereRadius;
    }
}
