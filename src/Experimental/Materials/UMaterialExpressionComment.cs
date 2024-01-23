using System;
using UnrealLib.Core;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.Materials;

public class UMaterialExpressionComment(FObjectExport export) : UMaterialExpression(export)
{
    [Property]                  public int PosX;
    [Property]                  public int PosY;
    [Property]                  public int SizeX;
    [Property]                  public int SizeY;
    [Property]                  public string Text;


}
