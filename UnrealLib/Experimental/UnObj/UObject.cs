using System.Collections.Generic;
using UnrealLib.Core;
using UnrealLib.Experimental.Component;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.UnObj;

// Feeling the limitations of separate UPK / UnrealArchive design...
// Many things are difficult or feel awkward to do when working with UObject derivatives.
// Hints at bad design? Maybe a static serializer would be better? How to convey package infos like version and state?

// All of these UObject implementations are very rough and shouldn't be used! Probably.

public class UObject
{
    #region Serialized members

    protected int NetIndex;
    protected List<FPropertyTag> DefaultProperties;

    #endregion

    #region Transient members

    internal FObjectExport Export { get; init; }
    public UnrealPackage Package => Export.Package;

    #endregion

    #region Accessors

    /// <summary>
    /// Returns the local name of this object.
    /// </summary>
    public string ObjectName => Export.GetName();
    /// <summary>
    /// Returns the full name of the export's Outer, or a null if no outer exists.
    /// </summary>
    public string? ObjectOuterName => Export.Outer?.GetFullName();
    /// <summary>
    /// Returns the full name of this object, including any outer objects.
    /// </summary>
    public string FullName => Export.GetFullName();

    #endregion

    public UObject(FObjectExport export)
    {
        Export = export;
    }

    // Position must be set before calling this method!
    public virtual void Serialize(UnrealArchive Ar)
    {
        if (Ar.IsLoading)
        {
            Ar.Position = Export.SerialOffset;
        }

        Ar.Serialize(ref NetIndex);

        // UClasses (null) and Components do not serialize script properties
        if (Export.Class is not null && this is not UComponent)
        {
            SerializeScriptProperties();
        }
    }

    // Until this is overridden explicitly within another class, all properties will be
    // placed in the generic DefaultProperties list, A.K.A the "I don't want to know about
    // this but don't want to toss it out" basket.
    public virtual void SerializeScriptProperties()
    {
        if (Ar.IsLoading)
        {
            DefaultProperties = new();
            
            while (true)
            {
                FPropertyTag Tag = new();
                Tag.Serialize(Ar);

                if (Tag.Name == "None") return;

                DefaultProperties.Add(Tag);
            }
        }
    }
}
