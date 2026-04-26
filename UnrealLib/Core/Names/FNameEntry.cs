using System;
using System.Collections.Generic;
using UnrealLib.Core.Interfaces;

namespace UnrealLib.Core;

public sealed class FNameEntry : ISerializable
{
    public const int MaxNameSize = 1024;
    private const ObjectFlags DefaultFlags = ObjectFlags.TagExp | ObjectFlags.LoadContextFlags;

    #region Serialized members

    private string _name = "";
    private ObjectFlags _flags;

    #endregion

    #region Transient members

    /// <summary>
    /// Tracks all object resources referencing this name entry.
    /// </summary>
    private readonly List<FObjectResource> _users = [];

    #endregion

    #region Constructors

    // Empty ctor required for serializer
    public FNameEntry() { }

    public FNameEntry(string name, ObjectFlags flags = DefaultFlags)
    {
        UDebug.Assert(!string.IsNullOrEmpty(name) && name.Length < MaxNameSize);

        _name = name;
        _flags = flags;
    }

    #endregion

    #region Accessors

    public string Name => _name;
    public ObjectFlags Flags => _flags;

//#if !SLIM_BUILD

    /// <summary>
    /// Creates and returns a <see cref="ReadOnlySpan{T}"/> over our Users array.
    /// </summary>
    public ReadOnlySpan<FObjectResource> GetUsers() => _users.AsSpan();
    public void ClearUsers() => _users.Clear();
    public int UserCount => _users.Count;

//#endif

    public override string ToString() => _name;

    #endregion

    public void UnsafeChangeName(string newName)
    {
        _name = newName;
    }

    public void Serialize(UnrealArchive Ar)
    {
        Ar.Serialize(ref _name);

        // Final release console builds have name flags set to 0
        Ar.Serialize(ref _flags);
    }

    /// <summary>
    /// Adds an object resource to our list of users.
    /// </summary>
    /// <param name="resource">The object resource to add to our list of users.</param>
    internal void Register(FObjectResource resource)
    {
        // Adding the same resource multiple times is not allowed!
        UDebug.Assert(!_users.Contains(resource));

        _users.Add(resource);
    }

    /// <summary>
    /// Removes an object resource from our list of users.
    /// </summary>
    /// <param name="resource">The object resource to remove from our list of users.</param>
    internal void Unregister(FObjectResource resource)
    {
        UDebug.Assert(_users.Contains(resource));

        _users.Remove(resource);
    }
}
