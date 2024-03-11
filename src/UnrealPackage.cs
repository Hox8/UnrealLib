using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealLib.Core;
using UnrealLib.Core.Compression;
using UnrealLib.Enums;
using UnrealLib.Experimental.Component;
using UnrealLib.Experimental.Components;
using UnrealLib.Experimental.Fonts;
using UnrealLib.Experimental.Materials;
using UnrealLib.Experimental.Mesh;
using UnrealLib.Experimental.Shaders;
using UnrealLib.Experimental.Shaders.New;
using UnrealLib.Experimental.Sound;
using UnrealLib.Experimental.Textures;
using UnrealLib.Experimental.UnObj;

namespace UnrealLib;

// Commonly-used names should be cached at package load
// This will likely be faster when we're dealing with lots of name comparisons e.g. dealing with UProperties
// A lazy-load approach where it's cached on first access sounds good

public class UnrealPackage : UnrealArchive
{
    // This should actually be tossed as soon as we exit ctor. Do not keep as a member!
    internal FPackageFileSummary Summary;

    // Following tables will be very, very difficult to change (must change all uc, other objects using it, etc...)
    public List<FNameEntry> Names = [];
    public List<FObjectImport> Imports;
    public List<FObjectExport> Exports;

    // internal List<UObject> ModifiedExports = [];

    // Names accessed via GetName() or GetOrAddName() are added here to speed up future accesses
    private readonly List<FNameEntry> RecentNames = [];

    /// <summary>
    /// List of offsets for each shader in this package's SeekFreeShaderCache.
    /// These are recorded on package load, and used during a full package save.
    /// </summary>
    internal int[]? SF_ShaderIndices;
    internal int[]? SF_MaterialIndices; // Only used by IB2 and newer?

    internal bool WasOriginallyCompressed;

    /// <summary>
    /// Whether to compress the package on save. Set to true if package was originally compressed.
    /// </summary>
    public bool ShouldSaveCompressed;

    /// <summary>
    /// Whether to perform a full (and significantly more expensive) save operation.
    /// </summary>
    public bool ShouldFullySerializePackage;

    #region Accessors

    /// <summary>Returns the package version this archive was saved with, e.g. 864.</summary>
    public int GetPackageVersion() => Summary.PackageVersion;
    /// <summary>Returns the UE3 version this package was saved with, e.g. 9714.</summary>
    public int GetEngineVersion() => Summary.EngineVersion;

    public CompressionFlags GetCompressionFlags() => Summary.CompressionFlags;
    public void SetCompressionFlags(CompressionFlags value) => Summary.CompressionFlags = value;

    public string FolderName { get => Summary.FolderName; set => Summary.FolderName = value; }

    #endregion

    #region Constructors
    private UnrealPackage(Stream stream) : base(stream) { }

    private UnrealPackage(string filePath, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, bool makeCopy = false) : base(filePath, mode, access, makeCopy: makeCopy)
    {
        if (HasError) return;

        try
        {
            Serialize(ref Summary);

            // Not supporting versions of UE3 older than IB1.
            ArgumentOutOfRangeException.ThrowIfLessThan(Summary.PackageVersion, Globals.PackageVerIB1);

            // @TODO do something elegant instead of this
            Version = Summary.PackageVersion == 864 ? 842 : Summary.PackageVersion;

            // Handle package compression
            if ((Summary.CompressionFlags & CompressionFlags.ZLib) != 0)
            {
                CompressionManager.DecompressPackage(this);
                WasOriginallyCompressed = true;
                ShouldSaveCompressed = true;
            }
            else if ((Summary.CompressionFlags & (CompressionFlags.LZO | CompressionFlags.LZX)) != 0)
            {
                SetError(ArchiveError.UnsupportedDecompress);
                return;
            }

            Debug.Assert(Position == Summary.NameOffset);

            CollectionsMarshal.SetCount(Names, Summary.NameCount);
            var span = CollectionsMarshal.AsSpan(Names);
            for (int i = 0; i < span.Length; i++)
            {
                Serialize(ref span[i]);
                span[i].Index = i;
            }

            // Serialize(ref Names, Summary.NameCount);
            Serialize(ref Imports, Summary.ImportCount);
            Serialize(ref Exports, Summary.ExportCount);

            // Depends map is nulled out for cooked packages, so skip over it
            // Position += Summary.ExportCount * 4;

            // @TODO thumbnail? Import/export guid?

            for (int i = 0; i < Imports.Count; i++) Imports[i].Link(this, i);
            for (int i = 0; i < Exports.Count; i++) Exports[i].Link(this, i);

            // Caching the initial offset is a hard requirement unless I implement full shader cache parsing
            if (FindObject("SeekFreeShaderCache") is FObjectExport shaderExport)
            {
                if (GetUObject(shaderExport, false) is Experimental.Shaders.New.UShaderCache shaderCache)
                {
                    Position = shaderExport.SerialOffset;
                    shaderCache.ProcessOffsets(this);
                }
            }
        }
        catch
        {
            SetError(ArchiveError.FailedParse, null);
        }
    }

    public static new UnrealPackage FromFile(string filePath, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, bool makeCopy = false) => new(filePath, mode, access, makeCopy);
    public static UnrealPackage CreateNew(Stream stream, short packageVer, short licenseeVer, int engineVer)
    {
        var upk = new UnrealPackage(stream)
        {
            Summary = new()
            {
                Tag = Globals.PackageTag,
                FolderName = "None",
                PackageVersion = packageVer,
                LicenseeVersion = licenseeVer,

                PackageFlags = PackageFlags.AllowDownload,
                
                Generations = [ new FGenerationInfo { ExportCount = 1, NameCount = 4, NetObjectCount = 1}],
                CompressionFlags = CompressionFlags.None,
                CompressedChunks = [],
                AdditionalPackagesToCook = [],
                TextureAllocations = { TextureTypes = [] },

                EngineVersion = engineVer
            },

            Names = [],
            Imports = [],
            Exports = []
        };

        upk.Imports.Add(new FObjectImport() { ClassPackage = upk.GetOrAddName("Core"), ClassName = upk.GetOrAddName("Class"), OuterIndex = -19, ObjectName = upk.GetOrAddName("Package")});
        upk.Exports.Add(new FObjectExport() { ClassIndex = -1, ObjectName = upk.GetOrAddName("TestObject"), GenerationNetObjectCount = [] });

        return upk;
    }

    #endregion

    #region IO

    public override long SaveToFile(string? path = null)
    {
        if (HasError) throw new Exception();

        StartSaving();

        Position = 0;

        // Fully serialize package
        if (ShouldFullySerializePackage)
        {
            using var dest = new UnrealPackage(File.Create(LastSavedFullPath + ".temp"));

            this.StartLoading();
            dest.StartSaving();

            dest.Summary = Summary;
            dest.Names = Names;
            dest.Imports = Imports;
            dest.Exports = Exports;
            dest.Version = Version;
            dest.SF_MaterialIndices = SF_MaterialIndices;
            dest.SF_ShaderIndices = SF_ShaderIndices;

            dest.StartSaving();

            dest.Serialize(ref Summary);

            Summary.NameOffset = (int)dest.Position;
            dest.Serialize(ref Names, Names.Count);

            Summary.ImportOffset = (int)dest.Position;
            dest.Serialize(ref Imports, Imports.Count);

            Summary.ExportOffset = (int)dest.Position;
            dest.Serialize(ref Exports, Exports.Count);

            Summary.DependsOffset = (int)dest.Position;
            dest.Position += Exports.Count * 4;
            
            Summary.NameCount = Names.Count;
            Summary.ImportCount = Imports.Count;
            Summary.ExportCount = Exports.Count;
            Summary.TotalHeaderSize = (int)dest.Position;
            Summary.ImportExportGuidsOffset = Summary.TotalHeaderSize;

            var class_Texture2D = FindObject("Engine.Texture2D");
            var class_LightmapTexture2D = FindObject("Engine.LightMapTexture2D");
            var class_SoundNodeWave = FindObject("Engine.SoundNodeWave");
            var class_StaticMesh = FindObject("Engine.StaticMesh");
            var class_SeekFreeShaderCache = FindObject("SeekFreeShaderCache");

            // var class_StaticMeshComponent = FindObject("Engine.StaticMeshComponent");
            var class_StaticMeshComponent = "Engine.StaticMeshComponent";

            List<string> skippedClasses = [];

            foreach (var export in Exports)
            {
                // Seek to export data in original stream
                Position = export.SerialOffset;

                // Set export data's offset to where it will be serialized in the destination stream
                export.SerialOffset = (int)dest.Position;

                // if (export.Class?.GetFullName() == "Engine.SkeletalMesh")
               // {
                 //   // var skmesh = (USkeletalMesh)GetUObject(export);

//                    throw new NotImplementedException("Engine.SkeletalMesh not supported!");
  //              }

                if (export.Class is not null && export.Class == class_Texture2D)
                {
                    using var texture = (UTexture2D)GetUObject(export);

                    // Serialize texture back to destination stream
                    texture.Serialize(dest);

                    //Console.ForegroundColor = ConsoleColor.Green;
                    //Console.Write($"[{export.Class.GetFullName()}] ");
                    //Console.ForegroundColor = ConsoleColor.Gray;

                    //Console.WriteLine($"Re-serialized {export.GetFullName()}...");
                }
                else if (export.Class is not null && export.Class == class_LightmapTexture2D)
                {
                    using var texture = (ULightMapTexture2D)GetUObject(export);

                    // Serialize texture back to destination stream
                    texture.Serialize(dest);

                    //Console.ForegroundColor = ConsoleColor.Green;
                    //Console.Write($"[{export.Class.GetFullName()}] ");
                    //Console.ForegroundColor = ConsoleColor.Gray;

                    //Console.WriteLine($"Re-serialized {export.GetFullName()}...");
                }
                else if (export.Class is not null && export.Class == class_SoundNodeWave)
                {
                    using var soundWave = (SoundNodeWave)GetUObject(export);

                    soundWave.Serialize(dest);

                    //Console.ForegroundColor = ConsoleColor.Green;
                    //Console.Write($"[{export.Class.GetFullName()}] ");
                    //Console.ForegroundColor = ConsoleColor.Gray;

                    //Console.WriteLine($"Re-serialized {export.GetFullName()}...");
                }
                else if (export.Class is not null && export.Class == class_StaticMesh)
                {
                    var staticMesh = (UStaticMesh)GetUObject(export);

                    staticMesh.Serialize(dest);

                    //Console.ForegroundColor = ConsoleColor.Green;
                    //Console.Write($"[{export.Class.GetFullName()}] ");
                    //Console.ForegroundColor = ConsoleColor.Gray;

                    //Console.WriteLine($"Re-serialized {export.GetFullName()}...");
                }
                //else if (export.Class is not null && export == class_SeekFreeShaderCache)
                //{
                //    _buffer.ConstrainedCopy(dest._buffer, export.SerialSize, 131072);

                //    if (dest.SF_ShaderIndices is not null)
                //    {
                //        dest.Position = export.SerialOffset;

                //        ((Experimental.Shaders.New.UShaderCache)dest.GetUObject(export, false)).ProcessOffsets(dest);
                //        Console.WriteLine("SHADERS FIXED");

                //        dest.Position = dest.Length;
                //    }
                //}
                else if (export.Class is not null && export.Class.GetFullName() == class_StaticMeshComponent)
                {
                    var smc = (UStaticMeshComponent)GetUObject(export);

                    smc.Serialize(dest);

                    //Console.ForegroundColor = ConsoleColor.Green;
                    //Console.Write($"[{export.Class.GetFullName()}] ");
                    //Console.ForegroundColor = ConsoleColor.Gray;

                    //Console.WriteLine($"Re-serialized {export.GetFullName()}...");
                }
                else
                {
                    // If it's not a type we can (or want to) serialize, do a dumb copy
                    _buffer.ConstrainedCopy(dest._buffer, export.SerialSize, 131072);

                    if (export.Class is not null && !skippedClasses.Contains(export.Class.GetFullName()))
                    {
                        skippedClasses.Add(export.Class.GetFullName());
                        // Console.WriteLine(skippedClasses[^1]);
                    }

                    //if (export.Class is not null && !export.Class.GetFullName().EndsWith("Property") && export.Class.GetFullName() != "Core.Function")
                    //{
                    //    Console.ForegroundColor = ConsoleColor.Yellow;
                    //    Console.Write($"[{export.Class?.GetFullName() ?? "UClass"}] ");
                    //    Console.ForegroundColor = ConsoleColor.Gray;
                    //    Console.WriteLine($"Blind-copied {export.GetFullName()}...");
                    //}
                }

                int oldSize = export.SerialSize;
                export.SerialSize = (int)dest.Position - export.SerialOffset;

                // Debug.Assert(oldSize == export.SerialSize);
            }

            // Reserialize new stats

            Debug.Assert(Names.Count == Summary.NameCount, "Name count changed during UObject serialization!");

            dest.Position = 0;
            dest.Serialize(ref Summary);

            Debug.Assert(Summary.NameOffset == dest.Position);
            dest.Serialize(ref Names, Summary.NameCount);

            Debug.Assert(Summary.ImportOffset == dest.Position);
            dest.Serialize(ref Imports, Summary.ImportCount);

            Debug.Assert(Summary.ExportOffset == dest.Position);
            dest.Serialize(ref Exports, Summary.ExportCount);

            Debug.Assert(Summary.DependsOffset == dest.Position);

            // Close src + dst streams
            _buffer.Dispose();
            dest._buffer.Dispose();

            // Overwrite and reopen src
            File.Move(dest.LastSavedFullPath, LastSavedFullPath, true);
            _buffer = File.Open(LastSavedFullPath, FileMode.Open, FileAccess.ReadWrite);
        }

        if (ShouldSaveCompressed)
        {
            base.SaveToFile(path);

            // Ugly hacky re-open underlying buffer.
            // I think I was doing this because decompressing sends bytes to ".temp", and I wanted to reuse the same filename. Just use something different...
            // @TODO true problem lies with De/Compress methods not dealing with temp streams properly.
            _buffer.Dispose();
            _buffer = File.Open(LastSavedFullPath, FileMode.Open, FileAccess.ReadWrite);

#if DEBUG
            long oldLength = Length;

            Console.Write($"Compressing {Name}...");  // So I don't forget to turn it off later
            var sw = Stopwatch.StartNew();
#endif
            CompressionManager.CompressPackage(this);
#if DEBUG
            sw.Stop();

            // string sizeString = $"{Globals.FormatSizeString(oldLength)} --> {Globals.FormatSizeString(Length)} ({(float)Length / oldLength:F4}%)";
            string sizeString = $"{(float)Length / oldLength:F3}%";
            string timeString = $"{sw.ElapsedMilliseconds}ms";

            Console.Write("\b\b\b");
            Console.SetCursorPosition(50, Console.CursorTop);
            Console.Write(sizeString);
            Console.SetCursorPosition(60, Console.CursorTop);
            Console.WriteLine(timeString);
#endif
        }

        return base.SaveToFile(path);
    }

    #endregion

    #region Names

    public FName GetOrAddName(string term)
    {
        if (!GetName(term, out FName name))
        {
            var nameEntry = new FNameEntry() { Name = term, Index = Names.Count, Flags = ObjectFlags.TagExp | ObjectFlags.LoadContextFlags };
            name = new FName(nameEntry, name?.Number ?? 0);

            Names.Add(nameEntry);
            RecentNames.Add(nameEntry);
        }

        return name;
    }

    #endregion

    public FObjectResource? GetObject(int objectIndex)
    {
        if (objectIndex > 0 && objectIndex <= Exports.Count)
        {
            return Exports[objectIndex - 1];
        }

        if (objectIndex < 0 && objectIndex <= Imports.Count)
        {
            return Imports[~objectIndex];
        }

        return null;
    }

    public FObjectExport? GetExport(int index) => (FObjectExport?)GetObject(index);
    public FObjectImport? GetImport(int index) => (FObjectImport?)GetObject(index);

    /// <summary>
    /// Retrieves a name from the name table at the specified offset.
    /// </summary>
    /// <param name="index">The offset within the name table to grab the FNameEntry from.</param>
    /// <returns>The FNameEntry at the specified offset.</returns>
    /// <exception cref="IndexOutOfRangeException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal FNameEntry GetNameEntry(int index)
    {
        Debug.Assert(index >= 0 && index < Names.Count, "Bad name table index");

        return Names[index];
    }

    /// <summary>
    /// Locates an <see cref="FObjectResource"/> within the Import and Export tables. @TODO this isn't quite true anymore...
    /// </summary>
    /// <remarks>
    /// - Passing an object's Outer is recommended to narrow down results, but is not required.<br/>
    /// - String comparison are case-insensitive.
    /// </remarks>
    /// <param name="searchTerm">The string name to search for. Can include the object's Outer. Case-insensitive.</param>
    /// <returns>The located <see cref="FObjectExport"/> or <see cref="FObjectImport"/> instance, or null if nothing was found.</returns>
    public FObjectResource? FindObject(string searchTerm)
    {
        string objectStr = searchTerm;
        string outerStr = "";

        bool withOuterName = false;

        // Handle any outer names
        int periodIndex = objectStr.LastIndexOf('.');
        if (periodIndex != -1)
        {
            objectStr = searchTerm[(periodIndex + 1)..];

            // If there are multiple outers, get the last one
            // Only care about the last outer; everything else is ignored
            int objIndex = searchTerm.AsSpan(0, periodIndex).LastIndexOf('.') + 1;
            outerStr = searchTerm[objIndex..periodIndex];

            withOuterName = true;
        }

        // Search for outer and leaf names
        // Conflicts can theoretically happen if two objects share the same leaf FName AND immediate parent FName (future me: wouldn't Number counteract this?)
        // (but different outer somewhere down the line). This will produce accurate results 99.999% of the time, which is fine for now.

        if (!GetName(objectStr, out var leafName) || (withOuterName && !GetName(outerStr, out _)))
        {
            return null;
        }

        foreach (var user in leafName.GetNameEntry().Users)
        {
            if (user.ObjectName == leafName)
            {
                // If we didn't specify an outer, use the first one we find
                if (!withOuterName) return user;

                // Otherwise if the leaf name and immediate parent name match, we've found the right object
                if (user.Outer is not null && user.Outer.ObjectName.ToString() == outerStr) return user;
            }
        }

        return null;
    }

    /// <summary>
    /// Looks for the corresponding <see cref="FNameEntry"/> within the names table.
    /// </summary>
    /// <remarks>
    /// - Automatically converts valid numbered-names into the appropriate format.<br/>
    /// - Case-insensitive.
    /// </remarks>
    public bool GetName(string searchTerm, out FName name)
    {
        // Don't process empty strings
        if (!string.IsNullOrEmpty(searchTerm))
        {
            // Convert name in case it ends with a number suffix (e.g. Name_14)
            FName.SplitName(searchTerm, out searchTerm, out int number);

            foreach (var entry in RecentNames)
            {
                if (entry.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    // This allows names to stay consistent with UEE
                    if (entry.bDoOffset) number++;

                    name = new FName(entry, number);
                    return true;
                }
            }

            foreach (var entry in Names)
            {
                if (entry.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    // This allows names to stay consistent with UEE
                    if (entry.bDoOffset) number++;

                    // Cache this name to speed up future accesses
                    RecentNames.Add(entry);

                    name = new FName(entry, number);
                    return true;
                }
            }
        }

        name = default;
        return false;
    }

    /// <summary>
    /// Replaces the passed export's UObject data entirely with that of the passed byte span.
    /// </summary>
    /// <remarks>
    /// Replacement UObject data must be complete-- containing UObject header, footer etc.
    /// </remarks>
    public void ReplaceExportData(FObjectExport export, ReadOnlySpan<byte> data)
    {
        StartSaving();

        // Update export size infos
        export.SerialSize = data.Length;
        export.SerialOffset = (int)Length;

        // Serialize the updated export entry
        Position = export.TableOffset;
        export.Serialize(this);

        // Serialize the export data at the end of the file
        Position = Length;
        Write(data);
    }

    /// <summary>
    /// Attempts to locate an <see cref="FObjectExport"/> at the specified file offset.
    /// </summary>
    public FObjectExport? GetObjectAtOffset(long offset)
    {
        foreach (var export in Exports)
        {
            if (export.SerialOffset + export.SerialSize >= offset)
                return export;
        }

        return null;
    }

    /// <remarks>
    /// Set position before calling this method!
    /// </remarks>
    public UObject GetUObject(FObjectExport export, bool shouldSerialize = true)
    {
        if (export.Object is null)
        {
            export.Object = export.Class?.GetName() switch
            {
                "Field" => new UField(export),
                "Function" => new UFunction(export),
                "State" => new UState(export),
                "Struct" => new UStruct(export),
                "Texture2D" => new UTexture2D(export),
                "LightMapTexture2D" => new ULightMapTexture2D(export),
                "SoundNodeWave" => new SoundNodeWave(export),
                // "Material" => new UMaterial(export),
                "Font" => new UFont(export),
                "ShaderCache" => new Experimental.Shaders.New.UShaderCache(export),
                "StaticMesh" => new UStaticMesh(export),

                // "Component" => new UComponent(export), isn't this abstract?
                "PrimitiveComponent" => new UPrimitiveComponent(export),
                "MeshComponent" => new UMeshComponent(export),
                "StaticMeshComponent" => new UStaticMeshComponent(export),

                "SkeletalMesh" => new USkeletalMesh(export),

                null => new UClass(export),     // UClass if class ref is null
                _ => throw new NotImplementedException($"'{export.Class}' is not implemented")
            };

            if (shouldSerialize)
            {
                Debug.Assert(IsLoading);

                // export.Package.Position = export.SerialOffset;
                export.Object.Serialize(export.Package);
            }
        }

        return export.Object;
    }
}
