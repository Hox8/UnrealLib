namespace UnrealLib.Core.Interfaces;

/// <summary>
/// Interface indicating that a class or struct can be serialized using a basic UnrealArchive stream.
/// Implementing this interface allows the object to define how it should be serialized.
/// </summary>
/// <remarks>
/// ISerializable cannot be implemented on unmanaged structs!
/// Due to the way C# generic constraint priority works, unmanaged
/// will always be preferred over ISerializable.
/// </remarks>
public interface ISerializable
{
    /// <summary>
    /// Serializes the object using the provided UnrealArchive stream.
    /// </summary>
    /// <param name="Ar">The UnrealArchive stream to serialize the object to.</param>
    void Serialize(UnrealArchive Ar);
}
