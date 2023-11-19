using System.IO;
using System.Runtime.CompilerServices;
using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FName : ISerializable
{
    // Serialized
    public int Index;
    public int Instance;
    
    // Transient
    internal FNameEntry Name;

    public void Serialize(UnrealStream stream)
    {
        if (stream.IsLoading)
        {
            stream.Serialize(ref Index);
        }
        else
        {
            stream.Serialize(ref Name.SerializedIndex);
        }
        
        stream.Serialize(ref Instance);
    }

    public void Serialize(UnrealStream stream, UnrealPackage pkg)
    {
        if (stream.IsLoading)
        {
            stream.Serialize(ref Index);
            Name = pkg.GetName(Index);
        }
        else
        {
            stream.Serialize(ref Name.SerializedIndex);
        }

        stream.Serialize(ref Instance);
    }

    /// <summary>
    /// Performs a case-insensitive comparison and returns the result. Name instance is ignored here!
    /// </summary>
    /// <remarks>
    /// FNames should NEVER contain Unicode characters. Optimizations have been done with this in mind.
    /// </remarks>
    // @TODO: This doesn't take name instances into account.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? b) => Name.Equals(b);

    public static bool operator ==(FName a, FName b) => a.Name == b.Name && a.Instance == b.Instance;
    public static bool operator !=(FName a, FName b) => !(a == b);
    public static bool operator ==(FName a, string b) => a.Equals(b);
    public static bool operator !=(FName a, string b) => !(a == b);

    public override string ToString() => Instance > 1 ? $"{Name.Name}_{Instance - 1}" : Name.Name;
    /*{
        string output = Name.Name;
        if (Instance > 1) output += $"_{Instance - 1}";

        return output;
    }*/
}
