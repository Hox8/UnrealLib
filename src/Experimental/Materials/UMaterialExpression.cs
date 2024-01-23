using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;

namespace UnrealLib.Experimental.Materials;

public class UMaterialExpression(FObjectExport export) : UObject(export)
{
    #region Structs

    public struct ExpressionInput
    {
        /// <summary>
        /// Material expression that this input is connected to, or NULL if not connected.
        /// </summary>
        public UMaterialExpression Expression;

        /// <summary>
        /// Index into Expression's outputs array that this input is connected to.
        /// </summary>
        public int OutputIndex;

        /// <summary>
        /// Optional name of the input.
        /// Note that this is the only member which is not derived from the output currently connected.
        /// </summary>
        public string InputName;
        public int Mask, MaskR, MaskG, MaskB, MaskA;
        // public int GCC64_Padding; // @todo 64: if the C++ didn't mismirror this structure (with MaterialInput), we might not need this
    };

    /// <summary>
    /// Struct that represents an expression's output.
    /// </summary>
    public struct ExpressionOutput
    {
        public string OutputName;
        public int Mask, MaskR, MaskG, MaskB, MaskA;
    };

    #endregion

    #region Properties

    /// <remarks>Editor-only.</remarks>
    public int MaterialExpressionEditorX, MaterialExpressionEditorY;

    /// <summary>
    /// Set to TRUE by RecursiveUpdateRealtimePreview() if the expression's preview needs to be updated in real-time in the material editor.
    /// </summary>
    public bool bRealtimePreview;

    /// <summary>
    /// Indicates that this is a 'parameter' type of expression and should always be loaded (ie not cooked away) because we might want the default parameter.
    /// </summary>
    public bool bIsParameterExpression;

    /// <summary>
    /// The material that this expression is currently being compiled in.
    /// This is not necessarily the object which owns this expression, for example a preview material compiling a material function's expressions.
    /// </summary>
    /// <remarks>Constant.</remarks>
    public UMaterial Material;

    /// <summary>
    /// The material function that this expression is being used with, if any.
    /// This will be NULL if the expression belongs to a function that is currently being edited, 
    /// </summary>
    public UMaterialFunction Function;

    /// <summary>
    /// A description that level designers can add (shows in the material editor UI).
    /// </summary>
    string Desc;

    /// <summary>
    /// Color of the expression's border outline.
    /// </summary>
    public Color BorderColor;

    /// <summary>
    /// If TRUE, use the output name as the label for the pin.
    /// </summary>
    public bool bShowOutputNameOnPin;
    /// <summary>
    /// If TRUE, do not render the preview window for the expression.
    /// </summary>
    public bool bHidePreviewWindow;

    /// <summary>
    /// Whether to draw the expression's inputs.
    /// </summary>
    public bool bShowInputs = true;

    /// <summary>
    /// Whether to draw the expression's outputs.
    /// </summary>
    public bool bShowOutputs = true;

    /// <summary>
    /// Categories to sort this expression into...
    /// </summary>
    public FName[] MenuCategories;

    /// <summary>
    /// The expression's outputs, which are set in default properties by derived classes.
    /// </summary>
    public ExpressionOutput[] Outputs = [new()];

    /// <summary>
    /// If TRUE, this expression is used when generating the StaticParameterSet.
    /// </summary>
    public bool bUsedByStaticParameterSet;

    #endregion
}
