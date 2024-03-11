using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Experimental.UnObj.DefaultProperties;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Infinity_Blade;

public partial class SaveFileMetaData : PropertyHolder
{
    #region Properties

    [UProperty] public string CurrentMap;
    [UProperty] public int CloudDocIndex = -1;
    [UProperty] public bool bDeleted;
    [UProperty] public int PawnLevel;
    [UProperty] public int CurrentGold;
    [UProperty] public int IsInNegativeBloodline;
    [UProperty] public int GenerationCount;
    [UProperty] public int NewPlusCount;
    [UProperty] public int UnlockedNewGamePlus;
    [UProperty] public ClashMobRewardData[] AvailableClashMobRewards;

    #endregion
}

public partial class SwordSaveSlotMetaData(FObjectExport export) : UObject(export)
{
    #region Properties

    [UProperty] public string? CharacterName;
    [UProperty] public string? FacebookAccount;
    [UProperty] public string? GameCenterAccount;
    [UProperty] public int CloudDocIndex = -1;
    [UProperty] public string? McpUniqueUserId;
    [UProperty] public SaveFileMetaData[] SaveFiles;   // OG (0) and NG+ (1)
    [UProperty] public int CurrentSaveFile;
    [UProperty] public int UpdateSaveCount;
    [UProperty] public ClashMobRewardData[]? PendingClashMobRewards;

    #endregion

    public override void Serialize(UnrealArchive Ar)
    {
        // First 4 bytes is supposed to be Version, but it seems to always be 0
        Ar.Position += 4;

        base.Serialize(Ar);
    }
}
