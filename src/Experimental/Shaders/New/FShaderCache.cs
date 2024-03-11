using System;
using System.Diagnostics;

namespace UnrealLib.Experimental.Shaders.New;

/// <summary>
/// A collection of persistent shaders.
/// </summary>
internal static class FShaderCache
{
    internal static void Process(UnrealPackage Ar, bool isLoading)
    {
        EShaderPlatform platform = default;
        Ar.Serialize(ref platform);

        if (Ar.Version < 796)
        {
            long[] dummy = default;
            Ar.Serialize(ref dummy);
        }

        // A dummy compressed cache is serialized for non-ps3/xbox shaders which we'll skip over here
        Debug.Assert(platform is not (EShaderPlatform.SP_PS3 or EShaderPlatform.SP_XBOXD3D));
        Ar.Position += 4;

        SerializeShaders(Ar, isLoading);
    }

    internal static void SerializeShaders(UnrealPackage Ar, bool isLoading)
    {
        int numShaders = default;
        Ar.Serialize(ref numShaders);

        if (isLoading)
        {
            Ar.SF_ShaderIndices = new int[numShaders];
        }

        int shaderStartPos = (int)Ar.Position;
        for (int i = 0; i < numShaders; i++)
        {
            // We aren't loading any values, so skip over everything
            Ar.Position += 8 + 16 + (Ar.Version >= 796 ? 20 : 0);

            int skipOffset = default;
            Ar.Serialize(ref skipOffset);

            // Record shader offset
            if (isLoading)
            {
                Ar.SF_ShaderIndices[i] = skipOffset - shaderStartPos;
            }
            else
            {
                // Update serialized skip offset with new offset

                skipOffset = shaderStartPos + Ar.SF_ShaderIndices[i];

                Ar.StartSaving();

                Ar.Position -= 4;
                Ar.Serialize(ref skipOffset);

                Ar.StartLoading();
            }

            // Skip to next shader
            Ar.Position = skipOffset;
        }
    }
}
