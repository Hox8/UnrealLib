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
    
    // @WARN: Comparison operators and ToString() expects _name reference to be valid!

    public static bool operator ==(FName a, FName b) => a.Name == b.Name && a.Instance == b.Instance;
    public static bool operator !=(FName a, FName b) => !(a == b);
    
    public override string ToString()
    {
        string output = Name.Name;
        if (Instance > 1) output += $"_{Instance - 1}";

        return output;
    }
}
