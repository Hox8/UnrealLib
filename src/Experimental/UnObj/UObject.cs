using System;
using System.Diagnostics;
using UnrealLib.Core;
using UnrealLib.Experimental.Component;
using UnrealLib.Experimental.UnObj.DefaultProperties;
using UnrealLib.Interfaces;

namespace UnrealLib.Experimental.UnObj;

public partial class UObject(FObjectExport? export = null) : PropertyHolder, ISerializable
{
    #region Serialized members

    protected int NetIndex;

    #endregion

    #region Transient members

    public FObjectExport? Export { get; init; } = export;
    public bool Loaded { get; internal set; }

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

    // Position must be set before calling this method!
    public virtual void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref NetIndex);

        // UClasses (null) and Components do not serialize script properties
        if (!Ar.SerializeBinaryProperties || Export.Class is not null)
        {
            SerializeScriptProperties((UnrealPackage)Ar);
        }

        Loaded = true;
    }

    public void EnsureLoaded(UnrealArchive Ar)
    {
        if (!Loaded)
        {
            Debug.Assert(Export is not null);

            var prePos = Ar.Position;

            Ar.Position = Export.SerialOffset;

            // Serialize() sets Loaded
            Serialize(Ar);

            Ar.Position = prePos;
        }
    }
}
