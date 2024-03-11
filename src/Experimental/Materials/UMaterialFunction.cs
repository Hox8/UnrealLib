using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Materials;

public partial class UMaterialFunction(FObjectExport? export = null) : UObject(export)
{
    #region UProperties

    /// <summary>
    /// Used by materials using this function to know when to recompile.
    /// </summary>
    [UProperty] protected FGuid StateId;

    /// <summary>
    /// Used in the material editor, points to the function asset being edited, which this function is just a preview for.
    /// </summary>
    /// <remarks>Editor-only.</remarks>
    [UProperty] protected UMaterialFunction ParentFunction;

    /// <summary>
    /// Description of the function which will be displayed as a tooltip wherever the function is used.
    /// </summary>
    [UProperty] public string Description;

    /// <summary>
    /// Whether to list this function in the material function library, which is a window in the material editor that lists categorized functions.
    /// </summary>
    [UProperty] public bool bExposeToLibrary;

    /// <summary>
    /// Categories that this function belongs to in the material function library.
    /// Ideally categories should be chosen carefully so that there are not too many.
    /// </summary>
    [UProperty] public string[] LibraryCategories;

    /// <summary>
    /// Array of material expressions, excluding Comments.  Used by the material editor.
    /// </summary>
    [UProperty] public UMaterialExpression[] FunctionExpressions;

    /// <summary>
    /// Array of comments associated with this material; viewed in the material editor.
    /// </summary>
    /// <remarks>Editor-only.</remarks>
    [UProperty] public UMaterialExpressionComment[] FunctionEditorComments;

    /// <summary>
    /// Transient flag used to track re-entrance in recursive functions like IsDependent.
    /// </summary>
    [UProperty] protected bool bReentrantFlag;

    #endregion
}
