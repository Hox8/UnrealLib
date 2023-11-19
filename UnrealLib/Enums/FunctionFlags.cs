using System;

namespace UnrealLib.Enums;

/// <summary>
/// Flags describing a UState object.
/// </summary>
[Flags]
public enum FunctionFlags : uint
{
    /// <summary>
    /// Function cannot be overridden (synonymous to C#'s 'sealed' keyword).
    /// </summary>
    /// <remarks>
    /// Functions using this keyword result in faster script code.
    /// </remarks>
    Final = 1U << 0,

    /// <summary>
    /// Function has been defined (not just declared).
    /// </summary>
	Defined = 1U << 1,

    /// <summary>
    /// Function is an iterator.
    /// </summary>
    Iterator = 1U << 2,

    /// <summary>
    /// Function is a latent state function.
    /// </summary>
    Latent = 1U << 3,

    /// <summary>
    /// Unary operator is a prefix operator.
    /// </summary>
    PreOperator = 1U << 4,

    /// <summary>
    /// Function cannot be reentered.
    /// </summary>
    Singular = 1U << 5,

    /// <summary>
    /// Function is network-replicated.
    /// </summary>
    Net = 1U << 6,

    /// <summary>
    /// Function should be sent reliably on the network.
    /// </summary>
    NetReliable = 1U << 7,

    /// <summary>
    /// Function executed on the client side.
    /// </summary>
    Simulated = 1U << 8,

    /// <summary>
    /// Executable from command line.
    /// </summary>
	Exec = 1U << 9,

    /// <summary>
    /// Native function.
    /// </summary>
	Native = 1U << 10,

    /// <summary>
    /// Event function.
    /// </summary>
	Event = 1U << 11,

    /// <summary>
    /// Operator function.
    /// </summary>
	Operator = 1U << 12,

    /// <summary>
    /// Static function.
    /// </summary>
	Static = 1U << 13,

    /// <summary>
    /// Function has optional parameters.
    /// </summary>
	HasOptionalParms = 1U << 14,

    /// <summary>
    /// Function doesn't modify this object.
    /// </summary>
	Const = 1U << 15,

    //= 1U << 16,	// unused

    /// <summary>
    /// Function is accessible in all classes (if overridden, parameters much remain unchanged).
    /// </summary>
    Public = 1U << 17,

    /// <summary>
    /// Function is accessible only in the class it is defined in (cannot be overridden, but function name may be reused in subclasses. IOW: if overridden, parameters don't need to match, and Super.Func() cannot be accessed since it's private.)
    /// </summary>
	Private = 1U << 18,

    /// <summary>
    /// Function is accessible only in the class it is defined in and subclasses (if overridden, parameters much remain unchanged).
    /// </summary>
	Protected = 1U << 19,
    /// <summary>
    /// Function is actually a delegate.
    /// </summary>
	Delegate = 1U << 20,
    /// <summary>
    /// Function is executed on servers (set by replication code if passes check).
    /// </summary>
	NetServer = 1U << 21,
    /// <summary>
    /// Function has out (pass by reference) parameters.
    /// </summary>
	HasOutParms = 1U << 22,
    /// <summary>
    /// Function has structs that contain defaults.
    /// </summary>
	HasDefaults = 1U << 23,
    /// <summary>
    /// Function is executed on clients.
    /// </summary>
	NetClient = 1U << 24,
    /// <summary>
    /// Function is imported from a DLL.
    /// </summary>
	DLLImport = 1U << 25,

    // Combinations of flags.
    FuncInherit = Exec | Event,
    FuncOverrideMatch = Exec | Final | Latent | PreOperator | Iterator | Static | Public | Protected | Const,
    NetFuncFlags = Net | NetReliable | NetServer | NetClient,
    AllFlags = uint.MaxValue,
};
