using NetEscapades.EnumGenerators;
using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;

namespace UnrealLib.Experimental.Component;

/// <summary>
/// Determines which ticking group an Actor/Component belongs to.
/// </summary>
[EnumExtensions]
public enum ETickingGroup
{
    /// <summary>Any item that needs to be updated before asynchronous work is done.</summary>
    TG_PreAsyncWork,
    /// <summary>Any item that can be run in parallel of our async work.</summary>
    TG_DuringAsyncWork,
    /// <summary>Any item that needs the async work to be done before being updated.</summary>
    TG_PostAsyncWork,
    /// <summary>Any item that needs the update work to be done before being ticked.</summary>
    TG_PostUpdateWork,
    /// <summary>Special effects that need to be updated last.</summary>
    TG_EffectsUpdateWork
}

public class UComponent(FObjectExport? export = null) : UObject(export)
{
    public int TemplateOwnerClass;
    public FName TemplateOwnerName;

    public override void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref TemplateOwnerClass);

        if ((Export.GetRoot().ObjectFlags & Enums.ObjectFlags.ClassDefaultObject) != 0)
        {
            // 'Default__' objects start with an FName of their class which we aren't interested in
            Ar.Serialize(ref TemplateOwnerName);
        }

        base.Serialize(Ar);
    }
}
