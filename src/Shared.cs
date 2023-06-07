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

    [Flags]
    public enum Game : byte
    {
        IB1 = 1 << 0,
        IB2 = 1 << 1,
        IB3 = 1 << 2,
        VOTE = 1 << 3,
        All = IB1 | IB2 | IB3 | VOTE
    };
}
