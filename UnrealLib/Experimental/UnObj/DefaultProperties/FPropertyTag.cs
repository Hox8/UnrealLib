using System;
using System.Runtime.InteropServices;
using UnrealLib.Core;

namespace UnrealLib.Experimental.UnObj.DefaultProperties
{
    // This class will store the metadata AND value for default properties.
    public class FPropertyTag // : ISerializable2
    {
        #region Cached names (dummy)

        // Placeholder to simulate having cached FNames (implement this later)
        // Name caching should follow a lazy-initialization approach
        // Cached names will also be tied to individual packages and not static.    -- that said, the strings will be constant, just not the indices... Helper struct?

        public const string NAME_None = "None";

        // Class property types
        public const string NAME_ByteProperty = "ByteProperty";
        public const string NAME_IntProperty = "IntProperty";
        public const string NAME_BoolProperty = "BoolProperty";
        public const string NAME_FloatProperty = "FloatProperty";
        public const string NAME_ObjectProperty = "ObjectProperty";
        public const string NAME_NameProperty = "NameProperty";
        public const string NAME_DelegateProperty = "DelegateProperty";
        public const string NAME_ClassProperty = "ClassProperty";
        public const string NAME_ArrayProperty = "ArrayProperty";
        public const string NAME_StructProperty = "StructProperty";
        public const string NAME_VectorProperty = "VectorProperty";
        public const string NAME_RotatorProperty = "RotatorProperty";
        public const string NAME_StrProperty = "StrProperty";
        public const string NAME_MapProperty = "MapProperty";
        public const string NAME_InterfaceProperty = "InterfaceProperty";

        #endregion

        public FName Name = new();
        public FName Type = new();

        public FName TargetName = new();

        internal int Size;
        internal int ArrayIndex;
        internal int ArraySize;

        internal FPropertyValue Value = new();

        public void Serialize(UnrealPackage Ar)
        {
            Name.Serialize(Ar);
            if (Name == NAME_None) return;

            Type.Serialize(Ar);
            Ar.Serialize(ref Size);
            Ar.Serialize(ref ArrayIndex);

            switch (Type.ToString())
            {
                case NAME_BoolProperty: Ar.Serialize(ref Value.Bool); break;
                case NAME_IntProperty: Ar.Serialize(ref Value.Int); break;
                case NAME_FloatProperty: Ar.Serialize(ref Value.Float); break;
                case NAME_StrProperty: Ar.Serialize(ref Value.String); break;
                case NAME_ObjectProperty: Ar.Serialize(ref Value.Int); break;
                case NAME_NameProperty: Ar.Serialize(ref Value.Name); break;

                case NAME_ByteProperty: TargetName.Serialize(Ar); Ar.Serialize(ref Value.Name); break;
                case NAME_StructProperty: TargetName.Serialize(Ar); SerializeStruct(); break;
                case NAME_ArrayProperty: SerializeArray(Ar); break;

                default: throw new NotImplementedException($"Unrecognized default property type '{Type}'");
            }
        }

        private void SerializeArray(UnrealArchive Ar)
        {
            Ar.Position += Size;
        }

        private void SerializeStruct() => throw new NotImplementedException();
        public override string ToString() => Name.ToString();
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct FPropertyValue
    {
        // Value types
        [FieldOffset(0)] internal bool Bool;
        [FieldOffset(0)] internal byte Byte;
        [FieldOffset(0)] internal int Int;
        [FieldOffset(0)] internal float Float;

        // Reference types (each 8 bytes on 64-bit)
        [FieldOffset(8)] internal string String;
        [FieldOffset(8)] internal FName Name;
    }
}
