using System;
using System.Collections.Generic;

namespace UnrealLib.Experimental.UnObj.DefaultProperties;

// @TODO this doesn't work with structs...
public abstract class PropertyHolder
{
#if KEEP_UNKNOWN_DEFAULT_PROPERTIES
    public List<FPropertyTag> DefaultProperties { get; set; }
#endif

    /// <returns>False if this was the last property to be serialized ("None"), otherwise True.</returns>
    public static bool GetNextProperty(UnrealArchive Ar, out FPropertyTag Tag)
    {
        Tag = new();

        // Tag::Serialize() returns False if we've hit final "None" property, otherwise True
        return Tag.Serialize(Ar);
    }

    // Overridable method allowing UObjects to map property things specific to them... NOT MEANT TO BE USED DIRECTLY!
    // @TODO write proper documentation!
    // @TODO this needs to be source-generated, with the option to manually specify (which can be useful for versioning)
    internal virtual void ParseProperty(UnrealArchive Ar, FPropertyTag tag) => HandleUnknownProperty(Ar, tag);

    // This is not meant to be overridden. Shared by all UObjects during property serialization;
    // This calls the class object's ParseProperty override when applicable
    public void SerializeProperties(UnrealArchive Ar)
    {
        // Keep pulling in properties until we hit "None"
        while (GetNextProperty(Ar, out var tag))
        {
            ParseProperty(Ar, tag);
        }
    }

    private void HandleUnknownProperty(UnrealArchive Ar, FPropertyTag Tag)
    {
        // At this point, we'll have read the property's metadata "tag", but not its actual value. Skip over it here.
        Ar.Position += Tag.Size;

#if KEEP_UNKNOWN_DEFAULT_PROPERTIES
        DefaultProperties ??= new();

        // Struct and array properties do not have the values stored!
        DefaultProperties.Add(Tag);

        Console.WriteLine($"Unknown property '{Tag.Name.GetString}' in '{this}'");
#endif
    }
}
