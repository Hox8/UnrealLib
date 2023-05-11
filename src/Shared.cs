namespace UnrealLib
{
    /// <summary>
    /// Marks a struct or class as compatible with deserialization
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDeserializable<T>
    {
        T Deserialize(UnrealStream unStream);
    }

    /// <summary>
    /// Marks a struct or class as compatible with serialization
    /// </summary>
    public interface ISerializable
    {
        void Serialize(UnrealStream unStream);
    }

    public enum Game : byte
    {
        IB3 = 0,
        IB2,
        IB1,
        VOTE
    }
}
