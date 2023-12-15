using UnrealLib.Core;
using UnrealLib.Experimental.UnObj;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.Infinity_Blade.SaveData_Classes;

public class SwordSaveGame(FObjectExport export) : UObject(export)
{
    #region Properties

    public int ValidSave;
    public int PawnLevel;
    public int CurrentXP;
    public int Health;
    public int HealthMax;
    public float CurrentSuperLevel;
    public float CurrentMagicLevel;
    public int CurrentGold;
    public int PurchasedGold;
    public int PurchasedGoldNextBuy;
    public int CurentPurchasedGold;
    public int[] NumConsumable = new int[41];
    public int PawnStatHealth;
    public int PawnStatShield;
    public int PawnStatDamage;
    public int PawnStatMagic;
    public int AvailableStatPoints;
    public int RebalanceStatsCount;
    public string[] EquippedItemNames;
    public string[] CurrentKeyItemList;
    public string[] UsedKeyItemList;
    public PlayerItemData[] PlayerInventory;
    public PlayerGemData[] PlayerUnequippedGems;
    public PlayerGemData[] CurrentStoreGems;
    public PlayerGemData[] PlayerCookerGems;
    public int GemCookerGameMonthsRequired;
    public int GemCookerGameMonthsElapsed;
    public string GemCookerRealStartTime;
    public int GemCookerRealTimeRequired;
    public int GemCookerGoldRequired;
    public int CurrentMapIndex;
    public string MapSaveName;
    public eNewWorldType StartNewWorld;
    public int IsInNegativeBloodline;
    public int GenerationCount;
    public int MaxGeneration;
    public int MinusWorldMaxGeneration;
    public int FightFinishedCount;
    public int PlayThroughFightFinishedCount;
    public int GodKingDefeated;
    public int DefeatedByGodKing;
    public int[] SuperBoss;
    public int[] GameFlagList;
    public int IsInInfiniteMode;
    public int NewPlusCount;
    public int MainGameFinishedCount;
    public int MaxMasteryCount;
    public int HasDoneLookTutorial;
    public int HasRatedGame;
    public int HasCheckedIB1;
    public int HasCheckedBook;
    public int HasCheckedIB3;
    public int HasCheckedBook2;
    public int HasSeenNewGamePlus;
    public int HasBonusStuff;
    public int UnlockedNewGamePlus;
    public int HasReadMPTutorial;
    public int HasUberStartingGear;
    public int HasDoneGemTutorial;
    public int TotalGoldAquired;
    public int TotalGoldFoundInWorld;
    public int TotalGoldFromBattle;
    public int TotalTreasureChest;
    public int MaxGodKingDefeatedLevel;
    public int YearsLater;
    public string ChoicePointSaveName;
    public int WorldLevel;
    public int WorldLevelAdjustment;
    public BossFixedData[] BossFixedWorldInfo;
    public NameAndValue[] TouchTreasureAwards;
    public int TotalTouchTreasureAwards;
    public string[] TreasureChestOpened;
    public string[] BossesGeneratedThisBloodline;
    public eElementalType[] PotentialBossElementalAttacks;
    public MapPersistentData[] PerLevelData;
    public string LastEquippedSnS;
    public string LastEquipped2S;
    public string LastEquipped2H;
    public int HeavyWeaponTutorial;
    public int DualWeaponTutorial;
    public int WorldTutorial;
    public int LowHealthTutorial;
    public int BloodlineResetCount;
    public int MultiBossMatches;
    public int MultiKnightMatches;
    public int MultiBossWins;
    public int MultiKnightWins;
    public int NewSurvivalFightMaxRound;
    public string[] CurrentBattleChallengeList;
    public BattleTrackingStats CurrentTotalTrackingStats;
    public int TotalGrabBagsUsed;
    public int TotalClashMobParticipation;
    public eTouchRewardActor[] ActiveBattlePotions;
    public eAchievements[] LoggedAnalyticsAchievements;
    public int SaveFileVersionInternal;
    public int SaveFileFixupVersion;
    public int bWasEncrypted;
    public int SaveFileOriginallyUnencrypted;
    public int PotentiallyManipulatedData;
    public int PotentialGoldHack;
    public PersistGameOptions GameOptions;
    public string CharacterName;
    public string OnSaleItem;
    public float SalePct;
    public int ShowSaleItemPopup;
    public int NumberOfSalesSeen;
    public int NumberOfSalesBought;
    public int NumberOfSalesSeenSinceBought;
    public int SaleTrackNumberItemsBought;
    public int SaleTrackNumberOfFights;
    public string SaveTag;
    public int SaveCreatedVersion;

    #endregion

    public void Serialize(UnrealArchive Ar)
    {
        // First 4 bytes is supposed to be Version, but it seems to always be 0
        Ar.Position += 4;

        base.Serialize(Ar);
    }
}
