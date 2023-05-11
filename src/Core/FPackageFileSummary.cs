namespace UnrealLib.Core
{
    public class FPackageFileSummary
    {
        public int Tag { get; set; }
        public short MainEngineVersion { get; set; }
        public short LicenseeVersion { get; set; }
        public int TotalHeaderSize { get; set; }
        public int PackageFlags { get; set; }
        public string FolderName { get; set; }

        public int NameCount { get; set; }
        public int NameOffset { get; set; }
        public int ExportCount { get; set; }
        public int ExportOffset { get; set; }
        public int ImportCount { get; set; }
        public int ImportOffset { get; set; }
        public int DependsOffset { get; set; }

        public int ImportExportGuidsOffset { get; set; }
        public int ImportGuidsCount { get; set; }
        public int ExportGuidsCount { get; set; }
        public int ThumbnailTableOffset { get; set; }

        public FGuid Guid { get; set; }
        public List<FGenerationInfo> Generations { get; set; }
        public int EngineVersion { get; set; }
        public int CookedContentVersion { get; set; }

        public int CompressionFlags { get; set; }
        public int PackageSource { get; set; }
        public List<FCompressedChunk> CompressedChunks { get; set; }

        public List<string> AdditionalPackagesToCook { get; set; }

        public List<FTextureType> TextureAllocations { get; set; }

        #region Constants

        public const int TAG = -1641380927;

        //public const int VER_ADDITIONAL_COOK_PACKAGE_SUMMARY = 516;
        //public const int VER_ASSET_THUMBNAILS_IN_PACKAGES = 584;
        //public const int VER_ADDED_CROSSLEVEL_REFERENCES = 623;
        //public const int VER_TEXTURE_PREALLOCATION = 767;

        #endregion

        public FPackageFileSummary Deserialize(UnrealStream unStream)
        {
            Tag = unStream.ReadInt32();
            if (Tag != TAG) return this;

            MainEngineVersion = unStream.ReadInt16();
            LicenseeVersion = unStream.ReadInt16();
            TotalHeaderSize = unStream.ReadInt32();
            FolderName = unStream.ReadFString();
            PackageFlags = unStream.ReadInt32();

            NameCount = unStream.ReadInt32();
            NameOffset = unStream.ReadInt32();
            ExportCount = unStream.ReadInt32();
            ExportOffset = unStream.ReadInt32();
            ImportCount = unStream.ReadInt32();
            ImportOffset = unStream.ReadInt32();
            DependsOffset = unStream.ReadInt32();

            ImportExportGuidsOffset = unStream.ReadInt32();
            ImportGuidsCount = unStream.ReadInt32();
            ExportGuidsCount = unStream.ReadInt32();

            ThumbnailTableOffset = unStream.ReadInt32();

            Guid = new FGuid().Deserialize(unStream);
            Generations = unStream.ReadObjectList<FGenerationInfo>();
            EngineVersion = unStream.ReadInt32();
            CookedContentVersion = unStream.ReadInt32();

            CompressionFlags = unStream.ReadInt32();
            CompressedChunks = unStream.ReadObjectList<FCompressedChunk>();
            PackageSource = unStream.ReadInt32();

            AdditionalPackagesToCook = unStream.ReadStringList();

            TextureAllocations = unStream.ReadObjectList<FTextureType>();

            return this;
        }
    }
}
