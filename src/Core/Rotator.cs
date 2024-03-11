using System.Runtime.InteropServices;

namespace UnrealLib.Core;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Rotator
{
    public readonly int Pitch, Yaw, Roll;

    public Rotator(int pitch, int yaw, int roll)
    {
        Pitch = pitch;
        Yaw = yaw;
        Roll = roll;
    }
}
