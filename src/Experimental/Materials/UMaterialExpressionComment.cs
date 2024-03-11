using UnrealLib.Core;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Materials;

public partial class UMaterialExpressionComment(FObjectExport? export = null) : UMaterialExpression(export)
{
    #region UProperties

    [UProperty] public int PosX, PosY;
    [UProperty] public int SizeX, SizeY;
    [UProperty] public string Text;

    #endregion
}
