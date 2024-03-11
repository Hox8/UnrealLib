using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Experimental.UnObj.DefaultProperties;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Materials;

#region "Structs"

public partial class ExpressionInput : PropertyHolder
{
    #region UProperties

    /// <summary>
    /// Material expression that this input is connected to, or NULL if not connected.
    /// </summary>
    [UProperty] public /*Pointer<UMaterialExpression>*/ int Expression;

    /// <summary>
    /// Index into Expression's outputs array that this input is connected to.
    /// </summary>
    [UProperty] public int OutputIndex;

    /// <summary>
    /// Optional name of the input.
    /// Note that this is the only member which is not derived from the output currently connected.
    /// </summary>
    [UProperty] public string InputName;

    [UProperty] public int Mask, MaskR, MaskG, MaskB, MaskA;

    #endregion
}

/// <summary>
/// Struct that represents an expression's output.
/// </summary>
public partial class ExpressionOutput : PropertyHolder
{
    #region UProperties

    [UProperty] public string OutputName;
    [UProperty] public int Mask, MaskR, MaskG, MaskB, MaskA;

    #endregion
}

#endregion

public partial class UMaterialExpression(FObjectExport? export = null) : UObject(export)
{
    #region UProperties

    /// <remarks>Editor-only.</remarks>
    [UProperty] public int MaterialExpressionEditorX, MaterialExpressionEditorY;

    /// <summary>
    /// Set to TRUE by RecursiveUpdateRealtimePreview() if the expression's preview needs to be updated in real-time in the material editor.
    /// </summary>
    [UProperty] public bool bRealtimePreview;

    /// <summary>
    /// Indicates that this is a 'parameter' type of expression and should always be loaded (ie not cooked away) because we might want the default parameter.
    /// </summary>
    [UProperty] public bool bIsParameterExpression;

    /// <summary>
    /// The material that this expression is currently being compiled in.
    /// This is not necessarily the object which owns this expression, for example a preview material compiling a material function's expressions.
    /// </summary>
    /// <remarks>Constant.</remarks>
    [UProperty] public UMaterial Material;

    /// <summary>
    /// The material function that this expression is being used with, if any.
    /// This will be NULL if the expression belongs to a function that is currently being edited, 
    /// </summary>
    [UProperty] public UMaterialFunction Function;

    /// <summary>
    /// A description that level designers can add (shows in the material editor UI).
    /// </summary>
    [UProperty] public string Desc;

    /// <summary>
    /// Color of the expression's border outline.
    /// </summary>
    [UProperty] public Color BorderColor;

    /// <summary>
    /// If TRUE, use the output name as the label for the pin.
    /// </summary>
    [UProperty] public bool bShowOutputNameOnPin;
    /// <summary>
    /// If TRUE, do not render the preview window for the expression.
    /// </summary>
    [UProperty] public bool bHidePreviewWindow;

    /// <summary>
    /// Whether to draw the expression's inputs.
    /// </summary>
    [UProperty] public bool bShowInputs = true;

    /// <summary>
    /// Whether to draw the expression's outputs.
    /// </summary>
    [UProperty] public bool bShowOutputs = true;

    /// <summary>
    /// Categories to sort this expression into...
    /// </summary>
    [UProperty] public FName[] MenuCategories;

    /// <summary>
    /// The expression's outputs, which are set in default properties by derived classes.
    /// </summary>
    [UProperty] public ExpressionOutput[] Outputs = [new()];

    /// <summary>
    /// If TRUE, this expression is used when generating the StaticParameterSet.
    /// </summary>
    [UProperty] public bool bUsedByStaticParameterSet;

    #endregion
}
