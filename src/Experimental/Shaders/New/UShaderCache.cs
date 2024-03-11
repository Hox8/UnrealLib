using System.Diagnostics;
using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;

namespace UnrealLib.Experimental.Shaders.New;

public class UShaderCache(FObjectExport? export = null) : UObject(export)
{
    /// <summary>
    /// - When called during the UPK constructor, constructs offset tables for shaders and material maps.<br/>
    /// - When called during a full package save, aforementioned offset tables are used to construct new offsets.<br/><br/>
    /// This allows shaders to persist across full package saves without needing to be re-serialized.
    /// </summary>
    public void ProcessOffsets(UnrealPackage Ar)
    {
        bool isLoading = Ar.IsLoading;
        Ar.StartLoading();

        base.Serialize(Ar);

        if (Ar.Version >= 805)
        {
            Ar.Position += 4;
        }

        FShaderCache.Process(Ar, isLoading);

        if (Ar.Version < 796)
        {
            long[] dummy = default;
            Ar.Serialize(ref dummy);
        }

        int numMaterialShaderMaps = default;
        Ar.Serialize(ref numMaterialShaderMaps);

        if (isLoading)
        {
            Ar.SF_MaterialIndices = new int[numMaterialShaderMaps];
        }

        int startPos = (int)Ar.Position;
        for (int i = 0; i < numMaterialShaderMaps; i++)
        {
            FStaticParameterSet staticParameters = default;
            Ar.Serialize(ref staticParameters);

            int shaderMapVesion = default, shaderMapLicenseeVersion = default;
            Ar.Serialize(ref shaderMapVesion);
            Ar.Serialize(ref shaderMapLicenseeVersion);

            int skipOffset = default;
            Ar.Serialize(ref skipOffset);

            // Record material offset
            if (isLoading)
            {
                Ar.SF_MaterialIndices[i] = skipOffset - startPos;
            }
            if (!isLoading)
            {
                // Update serialized skip offset with new offset

                skipOffset = startPos + Ar.SF_MaterialIndices[i];

                Ar.StartSaving();

                Ar.Position -= 4;
                Ar.Serialize(ref skipOffset);

                Ar.StartLoading();
            }

            // Skip to next material
            Ar.Position = skipOffset;
        }

        // Restore archive back to previous state
        if (!isLoading)
        {
            Ar.StartSaving();
        }
    }
}
