using System.Collections.Generic;
using UnrealLib.Config;
using UnrealLib.Core;
using UnrealLib.Enums;
using UnrealLib.Experimental.Textures;
using UnrealLib.Interfaces;

namespace UnrealLib.Experimental.Materials;

public class FMaterial : ISerializable
{
    #region Serialized members

    public string[] CompileErrors;

    /// <summary>
    /// The maximum texture dependency length for the material.
    /// </summary>
    public int MaxTextureDependencyLength;

    // UMaterialExpression*, int
    public KeyValuePair<int, int>[] TextureDependencyLengthMap;

    public FGuid Id;

    public uint NumUserTexCoords;

    // UTexture*
    public UTexture UniformExpressionTextures;

    public void Serialize(UnrealArchive Ar)
    {
        throw new System.NotImplementedException();
    }

    #endregion

    //public unsafe void Serialize(UnrealArchive _)
    //{
    //    var Ar = (UnrealPackage)_;

    //    Ar.Serialize(ref CompileErrors);

    //    // Editor-only. This should be cooked out (dummy TArray; count 0)
    //    Ar.Serialize(ref TextureDependencyLengthMap);

    //    Ar.Serialize(ref MaxTextureDependencyLength);
    //    Ar.Serialize(ref Id);
    //    Ar.Serialize(ref NumUserTexCoords);

    //    // Array of UTexture objects
    //    // 4 bytes array length
    //    // Each element is 4 bytes object index
    //    Ar.Serialize(ref UniformExpressionTextures);
    //    Ar.Position += 4;

    //    // Serializes a bunch of bitfield (4 bytes each??) properties...
    //}
}

public class FMaterialResource : FMaterial
{
    public EBlendMode BlendModeOverrideValue;
    public bool bIsBlendModeOverrided;
    public bool bIsMaskedOverrideValue;

    //public void Serialize(UnrealArchive Ar)
    //{
    //    base.Serialize(Ar);

    //    if (/*Ar.Version >= 853*/ true)
    //    {
    //        Ar.Serialize(ref BlendModeOverrideValue);
    //        Ar.Serialize(ref bIsBlendModeOverrided);
    //        Ar.Serialize(ref bIsMaskedOverrideValue);
    //    }
    //}
}
