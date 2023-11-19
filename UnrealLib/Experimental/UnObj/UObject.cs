using System.Collections.Generic;
using UnrealLib.Core;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.UnObj;

// Feeling the limitations of separate UPK / UnrealStream design...
// Many things are difficult or feel awkward to do when working with UObject derivatives.
// Hints at bad design? Maybe a static serializer would be better? How to convey package infos like version and state?

// All of these UObject implementations are very rough and shouldn't be used! Probably.

public class UObject(UnrealStream stream, UnrealPackage pkg, FObjectExport export)
{
    // Serialized
    protected int NetIndex;
    protected List<FPropertyTag> DefaultProperties;

    // Transient
    protected UnrealPackage _pkg = pkg;
    protected FObjectExport _export = export;

    public virtual void Serialize(UnrealStream stream)
    {
        if (stream.IsLoading)
        {
            stream.Position = _export.SerialOffset;
        }

        stream.Serialize(ref NetIndex);

        if (_export.Class is not null)
        {
            SerializeScriptProperties(stream, _pkg);
        }
    }

    // Should this be called first, then Serialize()? Probably make a wrapper method...
    public void Link(UnrealPackage pkg, FObjectExport export)
    {
        _export = export;
    }

    public void SerializeScriptProperties(UnrealStream stream, UnrealPackage pkg)
    {
        if (stream.IsLoading)
        {
            DefaultProperties = new();
            FPropertyTag Tag = new();

            while (true)
            {
                Tag.Serialize(stream, pkg);
                if (Tag.Name == "None") return;

                DefaultProperties.Add(Tag);
            }
        }
    }
}
