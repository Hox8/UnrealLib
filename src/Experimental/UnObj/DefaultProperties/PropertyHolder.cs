using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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

    internal static void SerializeArray<T>(ref T[] value, UnrealArchive Ar, FPropertyTag tag) where T : PropertyHolder, new()
    {
        if (Ar.IsLoading)
        {
            value = new T[value.Length];
        }

        for (int i = 0; i < tag.ArraySize; i++)
        {
            if (Ar.IsLoading)
            {
                value[i] = new();
            }

            value[i].SerializeProperties(Ar);
        }
    }

    // This is not meant to be overridden. Shared by all UObjects during property serialization;
    // This calls the class object's ParseProperty override when applicable
    public void SerializeProperties(UnrealArchive Ar)
    {
        // @TODO
        // NEW 22 DECEMBER 2023:
        //
        // IDEALLY:
        // Every class implementing PropertyHolder would implement its own linked list denoting order of properties, e.g.
        // (int) SizeX  > (int)SizeY > (string)TextureFileCacheName
        // READING properties would read along this order, skipping properties if they do not appear. This order should be guaranteed afaik.
        // WRITING properties would also follow this order, skipping properties if any match their default values.

        if (Ar.IsLoading)
        {
            // Keep pulling in properties until we hit "None"
            while (GetNextProperty(Ar, out var tag))
            {
                ParseProperty(Ar, tag);
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private void HandleUnknownProperty(UnrealArchive Ar, FPropertyTag Tag)
    {
        // At this point, we'll have read the property's metadata "tag", but not its actual value. Skip over it here.
        Ar.Position += Tag.Size - sizeof(int);

#if KEEP_UNKNOWN_DEFAULT_PROPERTIES
        DefaultProperties ??= new();

        // Struct and array properties do not have the values stored!
        DefaultProperties.Add(Tag);

        Console.WriteLine($"Unknown property '{Tag.Name.GetString}' in '{this}'");
#endif
    }
}
