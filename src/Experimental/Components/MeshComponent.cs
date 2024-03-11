using UnrealLib.Core;
using UnrealLib.Experimental.Materials;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Components;

public partial class UMeshComponent(FObjectExport? export = null) : UPrimitiveComponent(export)
{
    #region UProperties

    [UProperty(ArrayProperty = true, ObjectProperty = true)] public /*UMaterialInterface[]*/ int[] Materials;

    [UProperty] public new bool CastShadow = true;
    [UProperty] public new bool bAcceptsLights = true;
    [UProperty] public new bool bUseAsOccluder = true;

    #endregion
}
