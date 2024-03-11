using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UnrealLib.Experimental.Shaders;

internal class FShaderCache
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    record struct ShaderTypeCrcMapStruct
    {
        public long ShaderType;
        public uint Crc;
    }

    /// <summary>
    /// Map from shader type to the CRC that the shader type was compiled with.
    /// </summary>
    private ShaderTypeCrcMapStruct[] ShaderTypeCrcMap;

    /// <summary>
    /// Platform this shader cache is for.
    /// </summary>
    private byte Platform;

    internal void Load(UnrealArchive Ar, int offset)
    {
        Ar.Serialize(ref Platform);

        if (Ar.Version < 796)
        {
            Ar.Serialize(ref ShaderTypeCrcMap);
        }

        // Only XB and PS3 use compressed shader cache
        Debug.Assert(Platform != 1 && Platform != 2);

        // Skip over dummy compressed cache
        Ar.Position += 4;

        SerializeShaders(Ar, offset);
    }

    internal void SerializeShaders(UnrealArchive Ar, int offset)
    {
        if (Ar.IsLoading)
        {
            int numShaders = default;

            Ar.Serialize(ref numShaders);

            for (int shaderIndex = 0; shaderIndex < numShaders; shaderIndex++)
            {
                // Skip over the shader's type (8 bytes) and guid (16 bytes)
                Ar.Position += 8 + 16;

                if (Ar.Version > Globals.PackageVerIB1)
                {
                    // Skip over SHA hash. Not sure exactly when this was added
                    Ar.Position += 20;
                }

                // Deserialize the offset of the next shader.
                int skipOffset = default;
                Ar.Serialize(ref skipOffset);

                // Serialize updated skip offset
                Ar.StartSaving();
                Ar.Position -= 4;
                skipOffset += offset;
                Ar.Serialize(ref skipOffset);
                Ar.StartLoading();

                // Skip over the current shader
                Ar.Position = skipOffset;
            }
        }
    }
}
