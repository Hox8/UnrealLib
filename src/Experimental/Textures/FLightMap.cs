using System;
using System.Diagnostics;
using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Interfaces;

namespace UnrealLib.Experimental.Textures;

#region Enums

public enum ELightMapType : int
{
    LMT_None,
    LMT_1D,
    LMT_2D
}

#endregion

public abstract class FLightMap : ISerializable
{
    /// <summary>
    /// The GUIDs of lights which this light-map stores.
    /// </summary>
    public FGuid[] LightGuids;

    public virtual void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref LightGuids);
    }

    public static void Serialize(UnrealArchive Ar, ref FLightMap? R)
    {
        ELightMapType lightMapType = ELightMapType.LMT_None;

        if (!Ar.IsLoading)
        {
            if (R is FLightMap1D) lightMapType = ELightMapType.LMT_1D;
            else if (R is FLightMap2D) lightMapType = ELightMapType.LMT_2D;
        }

        Ar.Serialize(ref lightMapType);

        if (Ar.IsLoading)
        {
            R = lightMapType switch
            {
                ELightMapType.LMT_1D => new FLightMap1D(),
                ELightMapType.LMT_2D => new FLightMap2D(),
                _ => null
            };
        }

        R?.Serialize(Ar);
    }
}

public class FLightMap1D : FLightMap
{
    private Pointer<UObject> Owner;
    private FUntypedBulkData DirectionalSamples;
    private FUntypedBulkData SimpleSamples;
    private Vector[] ScaleVectors;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(ref Owner);
        Ar.Serialize(ref DirectionalSamples);

        Ar.Serialize(ref ScaleVectors, 3);

        Ar.Serialize(ref SimpleSamples);
    }
}

public class FLightMap2D : FLightMap
{
    protected Pointer<ULightMapTexture2D>[] Textures = new Pointer<ULightMapTexture2D>[3];
    protected Vector[] ScaleVectors = new Vector[3];
    protected FVector2 CoordinateScale;
    protected FVector2 CoordinateBias;

    public override void Serialize(UnrealArchive Ar)
    {
        base.Serialize(Ar);

        // Make '3' a constant somewhere. Also scalevector shared between 1D and 2D
        Ar.Serialize(ref Textures, 3);
        Ar.Serialize(ref ScaleVectors, 3);

        Ar.Serialize(ref CoordinateScale);
        Ar.Serialize(ref CoordinateBias);
    }
}
