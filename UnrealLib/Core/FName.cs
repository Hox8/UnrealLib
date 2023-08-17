using UnrealLib.Interfaces;

namespace UnrealLib.Core;

public class FName : ISerializable
{
    public int Index;
    public int Instance;
    
    // In-memory
    internal FNameEntry _name;

    public void Serialize(UnrealStream UStream)
    {
        if (UStream.IsLoading)
        {
            UStream.Serialize(ref Index);
        }
        else
        {
            UStream.Serialize(ref _name.SerializedIndex);
        }
        
        UStream.Serialize(ref Instance);
    }
    
    // @WARN: Comparison operators and ToString() expects _name reference to be valid!

    public static bool operator ==(FName a, FName b) => a._name == b._name && a.Instance == b.Instance;
    public static bool operator !=(FName a, FName b) => !(a == b);
    
    public override string ToString()
    {
        string output = _name.Name;
        if (Instance > 1) output += $"_{Instance - 1}";

        return output;
    }
}