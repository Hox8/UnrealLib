using UnrealLib.Core;
using UnrealLib.Experimental.Component;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.UnObj;

public class UObject : PropertyHolder
{
    #region Serialized members

    protected int NetIndex;

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
        //if (Ar.IsLoading)
        //{
        //    Ar.Position = Export.SerialOffset;
        //}

        Ar.Serialize(ref NetIndex);

        // UClasses (null) and Components do not serialize script properties
        if (!Ar.SerializeBinaryProperties || Export.Class is not null && this is not UComponent)
        {
            SerializeProperties(Ar);
        }
    }
}
