using UnrealLib.Core;

namespace UnrealLib.UnrealScript;

public class UDefaultProperty
{
    public FName Name; // Property name
    public FName Type; // Property type
    public int Size; // Property size (in bytes)
    public int ArrayIndex; // Index if type is array, else 0

    // Values
    public byte? BoolVal;
    public int? IntVal; // Shared with ObjRef type
    public float? FloatVal;
    public string? StrVal;

    public List<object>? ArrayVal;
    public FName? StructName; // Share with EnumName?
    public FName? EnumName;
    public FName? NameVal;

    public UDefaultProperty(UPK UPK) // Passing UPK instance is dirty
    {
        Name = UPK.UnStream.Read<FName>();
        if (Name == UPK.NAME_None) return;

        Type = UPK.UnStream.Read<FName>();
        Size = UPK.UnStream.ReadInt32();
        ArrayIndex = UPK.UnStream.ReadInt32();

        // Boolean
        if (Type == UPK.NAME_BoolProperty)
        {
            BoolVal = UPK.UnStream.ReadByte();
        }
        // Int32
        else if (Type == UPK.NAME_IntProperty)
        {
            IntVal = UPK.UnStream.ReadInt32();
        }
        // Float32 "Single"
        else if (Type == UPK.NAME_FloatProperty)
        {
            FloatVal = UPK.UnStream.ReadFloat();
        }
        // String
        else if (Type == UPK.NAME_StrProperty)
        {
            StrVal = UPK.UnStream.ReadFString();
        }

        // Array
        else if (Type == UPK.NAME_ArrayProperty)
        {
            // Unsupported as of yet. Skip over data
            int skipAmt = UPK.UnStream.ReadInt32();
            UPK.UnStream.Position += skipAmt;
        }
        // Struct
        else if (Type == UPK.NAME_StructProperty)
        {
            // Unsupported as of yet. Skip over data
            int skipAmt = UPK.UnStream.ReadInt32();
            UPK.UnStream.Position += skipAmt;

            // StructName = new FName().Deserialize(UPK.UnStream);
        }
        // Byte (Enum)
        else if (Type == UPK.NAME_ByteProperty)
        {
            EnumName = UPK.UnStream.Read<FName>();
            NameVal = UPK.UnStream.Read<FName>();
        }
        // FName
        else if (Type == UPK.NAME_NameProperty)
        {
            NameVal = UPK.UnStream.Read<FName>();
        }
        // Unrecognized property type
        else
        {
            throw new Exception($"{UPK.GetName(Type)} is an unsupported property type!");
        }
    }

    public void Serialize(UPK upk)
    {
        upk.UnStream.Write(Name.Serialize());
        if (Name == upk.NAME_None) return;
        
        upk.UnStream.Write(Type.Serialize());
        upk.UnStream.Write(Size);
        upk.UnStream.Write(ArrayIndex);

        // Boolean
        if (Type == upk.NAME_BoolProperty)
        {
            upk.UnStream.Write((byte)BoolVal);
        }
        // Int32
        else if (Type == upk.NAME_IntProperty)
        {
            upk.UnStream.Write((int)IntVal);
        }
        // Float32 "Single"
        else if (Type == upk.NAME_FloatProperty)
        {
            upk.UnStream.Write((float)FloatVal);
        }
        // String
        else if (Type == upk.NAME_StrProperty)
        {
            upk.UnStream.Write((string)StrVal, writeLength: true);
        }

        // Array
        else if (Type == upk.NAME_ArrayProperty)
        {
            throw new Exception("DefProp 'ArrayProperty' not supported");
        }
        // Struct
        else if (Type == upk.NAME_StructProperty)
        {
            throw new Exception("DefProp 'StructProperty' not supported");
        }
        // Byte (Enum)
        else if (Type == upk.NAME_ByteProperty)
        {
            upk.UnStream.Write(EnumName.Serialize());
            upk.UnStream.Write(NameVal.Serialize());
        }
        // FName
        else if (Type == upk.NAME_NameProperty)
        {
            upk.UnStream.Write(NameVal.Serialize());
        }
    }

    public string ToString(UPK upk)
    {
        if (Name == upk.NAME_None) return "None";
        
        return $"{upk.GetName(Name)} ({upk.GetName(Type)}: value)";
    }
}

// Base class of all objects
public abstract class UObject
{
    public int NetIndex { get; internal set; }
    public List<UDefaultProperty>? DefProps { get; internal set; }

    public UObject(UPK upk, FObjectExport export)
    {
        upk.UnStream.Position = export.SerialOffset;
        
        NetIndex = upk.UnStream.ReadInt32();
        
        // If export doesn't have a class index (ClassIndex == 0), it is a UClass
        if (export.ClassIndex != 0)
        {
            DefProps = new() { new UDefaultProperty(upk) };
            while (DefProps[^1].Name != upk.NAME_None)
            {
                DefProps.Add(new UDefaultProperty(upk));
            }
        }
    }

    public void Serialize(UPK upk)
    {
        // Assuming position is set beforehand
        upk.UnStream.Write(NetIndex);

        if (DefProps is null) return;
        foreach (var defProp in DefProps)
        {
            defProp.Serialize(upk);
        }
    }
}

// Base class of all UnrealScript objects
public abstract class UField : UObject
{
    public int Next { get; internal set; }

    public UField(UPK upk, FObjectExport export) : base(upk, export)
    {
        Next = upk.UnStream.ReadInt32();
    }

    public void Serialize(UPK upk)
    {
        base.Serialize(upk);
        
        upk.UnStream.Write(Next);
    }
}

public abstract class UStruct : UField
{
    public enum EStructFlags : int
    {
        // State flags.
        STRUCT_Native				= 0x00000001,
        STRUCT_Export				= 0x00000002,
        STRUCT_HasComponents		= 0x00000004,
        STRUCT_Transient			= 0x00000008,

        /** Indicates that this struct should always be serialized as a single unit */
        STRUCT_Atomic				= 0x00000010,

        /** Indicates that this struct uses binary serialization; it is unsafe to add/remove members from this struct without incrementing the package version */
        STRUCT_Immutable			= 0x00000020,

        /** Indicates that only properties marked config/globalconfig within this struct can be read from .ini */
        STRUCT_StrictConfig			= 0x00000040,

        /** Indicates that this struct will be considered immutable on platforms using cooked data. */
        STRUCT_ImmutableWhenCooked	= 0x00000080,

        /** Indicates that this struct should always be serialized as a single unit on platforms using cooked data. */
        STRUCT_AtomicWhenCooked		= 0x00000100,

        /** Struct flags that are automatically inherited */
        STRUCT_Inherit				= STRUCT_HasComponents|STRUCT_Atomic|STRUCT_AtomicWhenCooked|STRUCT_StrictConfig,
    };

    // enum EComponentInstanceFlags

    public int SuperStruct { get; internal set; }
    public int Child { get; internal set; }
    public int ScriptBytecodeSize { get; internal set; } // UC memory offset
    public int ScriptStorageSize { get; internal set; }  // Length of script in bytes
    public List<byte> Script { get; internal set; }

    public UStruct(UPK upk, FObjectExport export) : base(upk, export)
    {
        SuperStruct = upk.UnStream.ReadInt32();
        Child = upk.UnStream.ReadInt32();
        ScriptBytecodeSize = upk.UnStream.ReadInt32();
        ScriptStorageSize = upk.UnStream.ReadInt32();
        
        if (ScriptStorageSize > 0)
        {
            Script = upk.UnStream.ReadBytes(ScriptStorageSize).ToList();   
        }
    }

    public void Serialize(UPK upk)
    {
        base.Serialize(upk);
        
        upk.UnStream.Write(SuperStruct);
        upk.UnStream.Write(Child);
        upk.UnStream.Write(ScriptBytecodeSize);
        upk.UnStream.Write(ScriptStorageSize);
        upk.UnStream.Write(Script.ToArray());
    }
}

// I'm not sure this is actually used
public class UScriptStruct : UStruct
{
    public EStructFlags StructFlags { get; internal set; }
    public List<UDefaultProperty>? StructureDefaultProperties { get; internal set; }

    public UScriptStruct(UPK upk, FObjectExport export) : base(upk, export)
    {
        StructFlags = (EStructFlags)upk.UnStream.ReadInt32();
        
        StructureDefaultProperties = new() { new UDefaultProperty(upk) };
        while (StructureDefaultProperties[^1].Name != upk.NAME_None)
        {
            StructureDefaultProperties.Add(new UDefaultProperty(upk));
        }
    }
}

// An UnrealScript state
public class UFunction : UStruct
{
    [Flags]
    public enum EFunctionFlags : uint
{
	// Function flags.
	FUNC_Final				= 0x00000001,	// Function is final (prebindable, non-overridable function).
	FUNC_Defined			= 0x00000002,	// Function has been defined (not just declared).
	FUNC_Iterator			= 0x00000004,	// Function is an iterator.
	FUNC_Latent				= 0x00000008,	// Function is a latent state function.
	FUNC_PreOperator		= 0x00000010,	// Unary operator is a prefix operator.
	FUNC_Singular			= 0x00000020,   // Function cannot be reentered.
	FUNC_Net				= 0x00000040,   // Function is network-replicated.
	FUNC_NetReliable		= 0x00000080,   // Function should be sent reliably on the network.
	FUNC_Simulated			= 0x00000100,	// Function executed on the client side.
	FUNC_Exec				= 0x00000200,	// Executable from command line.
	FUNC_Native				= 0x00000400,	// Native function.
	FUNC_Event				= 0x00000800,   // Event function.
	FUNC_Operator			= 0x00001000,   // Operator function.
	FUNC_Static				= 0x00002000,   // Static function.
	FUNC_HasOptionalParms	= 0x00004000,	// Function has optional parameters
	FUNC_Const				= 0x00008000,   // Function doesn't modify this object.
	//						= 0x00010000,	// unused
	FUNC_Public				= 0x00020000,	// Function is accessible in all classes (if overridden, parameters much remain unchanged).
	FUNC_Private			= 0x00040000,	// Function is accessible only in the class it is defined in (cannot be overriden, but function name may be reused in subclasses.  IOW: if overridden, parameters don't need to match, and Super.Func() cannot be accessed since it's private.)
	FUNC_Protected			= 0x00080000,	// Function is accessible only in the class it is defined in and subclasses (if overridden, parameters much remain unchanged).
	FUNC_Delegate			= 0x00100000,	// Function is actually a delegate.
	FUNC_NetServer			= 0x00200000,	// Function is executed on servers (set by replication code if passes check)
	FUNC_HasOutParms		= 0x00400000,	// function has out (pass by reference) parameters
	FUNC_HasDefaults		= 0x00800000,	// function has structs that contain defaults
	FUNC_NetClient			= 0x01000000,	// function is executed on clients
	FUNC_DLLImport			= 0x02000000,	// function is imported from a DLL
	FUNC_K2Call				= 0x04000000,	// function can be called from K2
	FUNC_K2Override			= 0x08000000,	// function can be overriden/implemented from K2
	FUNC_K2Pure				= 0x10000000,	// function can be called from K2, and is also pure (produces no side effects). If you set this, you should set K2call as well.

	// Combinations of flags.
	FUNC_FuncInherit        = FUNC_Exec | FUNC_Event,
	FUNC_FuncOverrideMatch	= FUNC_Exec | FUNC_Final | FUNC_Latent | FUNC_PreOperator | FUNC_Iterator | FUNC_Static | FUNC_Public | FUNC_Protected | FUNC_Const,
	FUNC_NetFuncFlags       = FUNC_Net | FUNC_NetReliable | FUNC_NetServer | FUNC_NetClient,

	FUNC_AllFlags		= 0xFFFFFFFF,
};
    
    public short iNative { get; internal set; }
    public byte OperPrecedence { get; internal set; }    // Shortfix/postfix?
    public EFunctionFlags FunctionFlags { get; internal set; }
    public short RepOffset { get; internal set; }
    

    public UFunction(UPK upk, FObjectExport export) : base(upk, export)
    {
        iNative = upk.UnStream.ReadInt16();
        OperPrecedence = upk.UnStream.ReadByte();
        FunctionFlags = (EFunctionFlags)upk.UnStream.ReadInt32();

        if ((FunctionFlags & EFunctionFlags.FUNC_Net) != 0)
        {
            RepOffset = upk.UnStream.ReadInt16();
        }
    }
    
    public void Serialize(UPK upk)
    {
        base.Serialize(upk);
            
          upk.UnStream.Write(iNative);
          upk.UnStream.Write(OperPrecedence);
          upk.UnStream.Write((int)FunctionFlags);
          if ((FunctionFlags & EFunctionFlags.FUNC_Net) != 0)
          {
              upk.UnStream.Write(RepOffset);
          }
    }
}

// An UnrealScript state
public class UState : UStruct
{
    [Flags]
    public enum EStateFlags
    {
        // State flags.
        STATE_Editable		= 0x00000001,	// State should be user-selectable in UnrealEd.
        STATE_Auto			= 0x00000002,	// State is automatic (the default state).
        STATE_Simulated     = 0x00000004,   // State executes on client side.
    };
    
    /* List of functions currently probed by the current class (see UnNames.h) */
    public int ProbeMask { get; internal set; }

    /* Offset into Script array that contains the table of FLabelEntry's */
    public short LabelTableOffset { get; internal set; }

    /* Active state flags (see UnStack.h EStateFlags) */
    public EStateFlags StateFlags { get; internal set; }

    /* Map of all functions by name contained in this state */
    public Dictionary<FName, int> FuncMap { get; internal set; }

    
    public UState(UPK upk, FObjectExport export) : base(upk, export)
    {
        ProbeMask = upk.UnStream.ReadInt32();
        LabelTableOffset = upk.UnStream.ReadInt16();
        StateFlags = (EStateFlags)upk.UnStream.ReadInt32();

        // Read function map
        int funcMapSize = upk.UnStream.ReadInt32();
        FuncMap = new Dictionary<FName, int>(funcMapSize);
        for (int i = 0; i < funcMapSize; i++)
        {
            FuncMap.Add(new FName().Deserialize(upk.UnStream), upk.UnStream.ReadInt32());
        }
    }
}

/// <summary>
/// An object class
/// </summary>
public class UClass : UState
{
    public int ClassFlags { get; internal set; }
    public int ClassWithin { get; internal set; }
    public FName ClassConfigName { get; internal set; }
    
    /* A mapping of the component template names inside this class to the template itself */
    public Dictionary<FName, int> ComponentNameToDefaultObjectMap { get; internal set; }
    
    /*
	 * The list of interfaces which this class implements, along with the pointer property that is located at the offset of the interface's vtable.
	 * If the interface class isn't native, the property will be NULL.
	 */
    public Dictionary<int, int> Interfaces { get; internal set; }
    public FName DllBindName { get; internal set; }
    public int ClassDefaultObject { get; internal set; }

    public UClass(UPK upk, FObjectExport export) : base(upk, export)
    {
        ClassFlags = upk.UnStream.ReadInt32();
        ClassWithin = upk.UnStream.ReadInt32();
        ClassConfigName = new FName().Deserialize(upk.UnStream);

        int size = upk.UnStream.ReadInt32();
        ComponentNameToDefaultObjectMap = new(size);
        for (int i = 0; i < size; i++)
        {
            ComponentNameToDefaultObjectMap.Add(
                new FName().Deserialize(upk.UnStream), upk.UnStream.ReadInt32());
        }

        size = upk.UnStream.ReadInt32();
        Interfaces = new(size);
        for (int i = 0; i < size; i++)
        {
            Interfaces.Add(upk.UnStream.ReadInt32(), upk.UnStream.ReadInt32());
        }

        DllBindName = new FName().Deserialize(upk.UnStream);
        ClassDefaultObject = upk.UnStream.ReadInt32();
    }
}