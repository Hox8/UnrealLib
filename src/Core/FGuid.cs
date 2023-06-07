namespace UnrealLib.Core
{
    /// <summary>
    /// @TODO Describe
    /// Is a struct, so pass by reference!
    /// </summary>
    public class FGuid : IDeserializable<FGuid>
    {
        public int A { get; internal set; }
        public int B { get; internal set; }
        public int C { get; internal set; }
        public int D { get; internal set; }

        public FGuid Deserialize(UnrealStream stream)
        {
            A = stream.ReadInt32();
            B = stream.ReadInt32();
            C = stream.ReadInt32();
            D = stream.ReadInt32();
            return this;
        }

        public void Serialize(UnrealStream stream)
        {
            stream.Write(A);
            stream.Write(B);
            stream.Write(C);
            stream.Write(D);
        }

        public override string ToString()
        {
            return $"{A:X8}-{B:X8}-{C:X8}-{D:X8}";
        }
    }
}