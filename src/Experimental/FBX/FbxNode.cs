using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnrealLib.Interfaces;

namespace UnrealLib.Experimental.FBX;

public class FbxNode : ISerializable
{
    public int EndOffset;
    public int NumProperties;
    public int PropertyListLength;  // Total length in bytes of all properties
    public byte NameLength;
    public string Name;

    public List<FbxNode> Children;
    public List<FbxPropertyValue> Properties;

    public void Serialize(UnrealArchive Ar)
    {
        int offsetStart = (int)Ar.Position;
        NumProperties = Properties?.Count ?? 0;

        Ar.Serialize(ref EndOffset);
        Ar.Serialize(ref NumProperties);
        Ar.Serialize(ref PropertyListLength);
        Ar.Serialize(ref NameLength);
        Ar.SerializeFbxString(ref Name, NameLength);
        bool isNodeNull = Name is null;

        // Serialize properties
        if (NumProperties > 0)
        {
            if (Ar.IsLoading)
            {
                // Initialize Properties list
                Properties = [];
                CollectionsMarshal.SetCount(Properties, NumProperties);
            }
            else
            {
                // We'll use this offset to calculate length once we've written out all properties
                PropertyListLength = (int)Ar.Position;
            }

            var span = CollectionsMarshal.AsSpan(Properties);
            for (int i = 0; i < NumProperties; i++)
            {
                span[i].Serialize(Ar);
            }

            if (!Ar.IsLoading)
            {
                PropertyListLength = (int)Ar.Position - PropertyListLength;
            }
        }

        // Serialize nested nodes
        if (Ar.IsLoading)
        {
            // Do we have nodes to process?
            if (Ar.Position < EndOffset)
            {
                Children = [];

                while (Ar.Position < EndOffset)
                {
                    Children.Add(new());
                    Children[^1].Serialize(Ar);
                }
            }
        }
        else
        {
            // Serialize all children, including the "null terminator"
            for (int i = 0; i < Children?.Count; i++)
            {
                Children[i].Serialize(Ar);
            }
        }

        // Write out node header stats if node is not a null terminator
        if (!Ar.IsLoading && !isNodeNull)
        {
            NameLength = (byte)Encoding.UTF8.GetByteCount(Name);
            Debug.Assert(NameLength <= byte.MaxValue, "Node names must be <= 255 bytes in length!");

            EndOffset = (int)Ar.Position;
            Ar.Position = offsetStart;

            Ar.Serialize(ref EndOffset);
            Ar.Serialize(ref NumProperties);
            Ar.Serialize(ref PropertyListLength);
            Ar.Serialize(ref NameLength);
            Ar.SerializeFbxString(ref Name, NameLength);

            Ar.Position = EndOffset;
        }
    }

    #region Public API

    public FbxNode? this[string name] => Children?.Find(node => node.Name == name);

    // C# is complaining if Properties is accessed through above accessor. This is a (dangerous) solution
    internal Span<FbxPropertyValue> GetProperties() => CollectionsMarshal.AsSpan(Properties);

    #endregion

    public override string ToString() => Name ?? "NULL";
}
