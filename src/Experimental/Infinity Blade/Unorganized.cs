namespace UnrealLib.Experimental.Infinity_Blade;

public struct PlayerItemData
{
    public string ItemName;
    public int XpGainedFromItem;
    public int NumberPlayerHas;
    public int Mastered;
    public PlayerGemData[] SocketedGemData;
}

public struct PlayerGemData
{
    public string GemName;
    public byte GemTier;
    public byte CookedGemVar;
    public float RandomAddPct;
}

public struct BossFixedData()
{
    public string BossSpawnPointName;
    public int BossClass;
    public int BossArchetype;
    public int BossLevel;
    public int BossXpLevel;
    public eTouchRewardActor BossForceDropType = eTouchRewardActor.TRA_Random;
    public string BossFixedDrop;
    public int RandSeedStage1;
    public int RandSeedStage2;
    public int RandSeedStage3;
    public float[] BossElementalRandList;
    public int BossInstanceKillCount;
    public int BossSpawnFixedListIndex;
}

public struct NameAndValue
{
    public string ObjName;
    public int Value;
}

public struct MapPersistentData
{
    public bool bGeneratedThisBloodline;
    public int TimesGenerated;
    public int GenPotions;
    public int GenKeys;
    public NameAndValue[] PersistActorCounts;
    public NameAndValue[] DontClearPersistActorCounts;
};

public struct BattleTrackingStats
{
    public int TotalGoodHits;
    public int TotalScratch;
    public int TotalComboCount;
    public int TotalBlockCount;
    public int TotalPerfectBlockCount;
    public int TotalDodgeCount;
    public int TotalParryCount;
    public int TotalPerfectParryCount;
    public int TotalRealPerfectParryCount;
    public int TotalStabCount;
    public int TotalSlashCount;
    public int TotalPerfectSlashCount;
    public int TotalMagicSpellCount;
    public int TotalSuperAttackCount;
    public int TotalDodgeFailedCount;
    public int TotalParryFailedCount;
    public int TotalMagicFailedCount;
    public int TotalBlockFailedCount;
    public int TotalMissCount;
}