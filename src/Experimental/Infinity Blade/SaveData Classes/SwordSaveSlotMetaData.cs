using System;
using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.Infinity_Blade;

public class SaveFileMetaData : PropertyHolder
{
    #region Properties

    public string CurrentMap;
    public int CloudDocIndex = -1;
    public bool bDeleted;
    public int PawnLevel;
    public int CurrentGold;
    public int IsInNegativeBloodline;
    public int GenerationCount;
    public int NewPlusCount;
    public int UnlockedNewGamePlus;
    public ClashMobRewardData[] AvailableClashMobRewards;

    #endregion

    internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
    {
        switch (tag.Name.GetString)
        {
            case nameof(CurrentMap): Ar.Serialize(ref CurrentMap); break;
            case nameof(CloudDocIndex): Ar.Serialize(ref CloudDocIndex); break;
            case nameof(bDeleted): Ar.Serialize(ref bDeleted); break;
            case nameof(PawnLevel): Ar.Serialize(ref PawnLevel); break;
            case nameof(CurrentGold): Ar.Serialize(ref CurrentGold); break;
            case nameof(IsInNegativeBloodline): Ar.Serialize(ref IsInNegativeBloodline); break;
            case nameof(GenerationCount): Ar.Serialize(ref GenerationCount); break;
            case nameof(NewPlusCount): Ar.Serialize(ref NewPlusCount); break;
            case nameof(UnlockedNewGamePlus): Ar.Serialize(ref UnlockedNewGamePlus); break;
            case nameof(AvailableClashMobRewards): throw new NotImplementedException();
            default: base.ParseProperty(Ar, tag); break;
        }
    }
}

public class SwordSaveSlotMetaData(FObjectExport export) : UObject(export)
{
    #region Properties

    public string? CharacterName;
    public string? FacebookAccount;
    public string? GameCenterAccount;
    public int CloudDocIndex = -1;
    public string? McpUniqueUserId;
    public SaveFileMetaData[] SaveFiles = [new(), new()];   // OG (0) and NG+ (1)
    public int CurrentSaveFile;
    public int UpdateSaveCount;
    public ClashMobRewardData[]? PendingClashMobRewards;

    #endregion

    public override void Serialize(UnrealArchive Ar)
    {
        // First 4 bytes is supposed to be Version, but it seems to always be 0
        Ar.Position += 4;

        base.Serialize(Ar);
    }

    internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
    {
        switch (tag.Name.GetString)
        {
            case nameof(CharacterName): Ar.Serialize(ref CharacterName); break;
            case nameof(FacebookAccount): Ar.Serialize(ref FacebookAccount); break;
            case nameof(GameCenterAccount): Ar.Serialize(ref GameCenterAccount); break;
            case nameof(CloudDocIndex): Ar.Serialize(ref CloudDocIndex); break;
            case nameof(McpUniqueUserId): Ar.Serialize(ref McpUniqueUserId); break;
            case nameof(SaveFiles): SaveFiles[tag.ArrayIndex].SerializeProperties(Ar); break;
            case nameof(CurrentSaveFile): Ar.Serialize(ref CurrentSaveFile); break;
            case nameof(UpdateSaveCount): Ar.Serialize(ref UpdateSaveCount); break;
            case nameof(PendingClashMobRewards): throw new NotImplementedException();
            default: base.ParseProperty(Ar, tag); break;
        }
    }
}
