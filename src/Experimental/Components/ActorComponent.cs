using UnrealLib.Core;
using UnrealLib.Experimental.Component;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Components;

public partial class UActorComponent(FObjectExport? export = null) : UComponent(export)
{
    #region UProperties

    [UProperty] public bool bTickInEditor;

    /// <summary>The ticking group this component belongs to.</summary>
    [UProperty] public ETickingGroup TickGroup = ETickingGroup.TG_DuringAsyncWork;

    #endregion
}
