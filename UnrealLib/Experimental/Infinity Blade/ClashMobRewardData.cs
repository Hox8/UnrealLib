using UnrealLib.Core;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.Infinity_Blade;

public class ClashMobRewardData : PropertyHolder
{
    public FName SocialChallengeId;
    public int RewardsGiven;
    public eTouchRewardActor RewardType = eTouchRewardActor.TRA_Random;
    public string RewardData;
    public ClashMobRewardState PendingState = ClashMobRewardState.CMRS_NoState;

    internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
    {
        switch (tag.Name.GetString)
        {
            case nameof(SocialChallengeId): Ar.Serialize(ref SocialChallengeId); break;
            case nameof(RewardsGiven): Ar.Serialize(ref RewardsGiven); break;
            case nameof(RewardType): Ar.Serialize(ref tag.Value.Name); RewardType = GetRewardType(tag.Value.Name); break;
            case nameof(RewardData): Ar.Serialize(ref RewardData); break;
            case nameof(PendingState): Ar.Serialize(ref tag.Value.Name); PendingState = GetClashMobRewardState(tag.Value.Name); break;
            default: base.ParseProperty(Ar, tag); break;
        }
    }

    private ClashMobRewardState GetClashMobRewardState(FName name) => name.GetString switch
    {
        "CMRS_NoState" => ClashMobRewardState.CMRS_NoState,
        "CMRS_PendingServerAcknowledge" => ClashMobRewardState.CMRS_PendingServerAcknowledge,
        "CMRS_PendingPlayerAcknowledge" => ClashMobRewardState.CMRS_PendingPlayerAcknowledge,
        "CMRS_PendingGiveToPlayer" => ClashMobRewardState.CMRS_PendingGiveToPlayer
    };

    private eTouchRewardActor GetRewardType(FName name) => name.GetString switch
    {
        "TRA_Random" => eTouchRewardActor.TRA_Random,
        "TRA_Random_Potion" => eTouchRewardActor.TRA_Random_Potion,
        "TRA_Random_Gold" => eTouchRewardActor.TRA_Random_Gold,
        "TRA_Random_Key" => eTouchRewardActor.TRA_Random_Key,
        "TRA_Random_Gem" => eTouchRewardActor.TRA_Random_Gem,
        "TRA_Random_Item" => eTouchRewardActor.TRA_Random_Item,
        "TRA_None" => eTouchRewardActor.TRA_None,
        "TRA_Gold_Small" => eTouchRewardActor.TRA_Gold_Small,
        "TRA_Gold_Medium" => eTouchRewardActor.TRA_Gold_Medium,
        "TRA_Gold_Large" => eTouchRewardActor.TRA_Gold_Large,
        "TRA_Key_Small" => eTouchRewardActor.TRA_Key_Small,
        "TRA_Key_Medium" => eTouchRewardActor.TRA_Key_Medium,
        "TRA_Key_Large" => eTouchRewardActor.TRA_Key_Large,
        "TRA_Key_Item" => eTouchRewardActor.TRA_Key_Item,
        "TRA_Gem_Fixed" => eTouchRewardActor.TRA_Gem_Fixed,
        "TRA_Item_Fixed" => eTouchRewardActor.TRA_Item_Fixed,
        "TRA_Item_Weapon" => eTouchRewardActor.TRA_Item_Weapon,
        "TRA_Item_Shield" => eTouchRewardActor.TRA_Item_Shield,
        "TRA_Item_Armor" => eTouchRewardActor.TRA_Item_Armor,
        "TRA_Item_Helmet" => eTouchRewardActor.TRA_Item_Helmet,
        "TRA_Item_Magic" => eTouchRewardActor.TRA_Item_Magic,
        "TRA_GrabBag_Small" => eTouchRewardActor.TRA_GrabBag_Small,
        "TRA_GrabBag_Medium" => eTouchRewardActor.TRA_GrabBag_Medium,
        "TRA_GrabBag_Large" => eTouchRewardActor.TRA_GrabBag_Large,
        "TRA_GrabBag_SmallGem" => eTouchRewardActor.TRA_GrabBag_SmallGem,
        "TRA_GrabBag_MediumGem" => eTouchRewardActor.TRA_GrabBag_MediumGem,
        "TRA_GrabBag_LargeGem" => eTouchRewardActor.TRA_GrabBag_LargeGem,
        "TRA_GrabBag_Uber" => eTouchRewardActor.TRA_GrabBag_Uber,
        "TRA_Potion_HealthL" => eTouchRewardActor.TRA_Potion_HealthL,
        "TRA_Potion_HealthRegen" => eTouchRewardActor.TRA_Potion_HealthRegen,
        "TRA_Potion_ShieldRegen" => eTouchRewardActor.TRA_Potion_ShieldRegen,
        "TRA_Potion_EasyParry" => eTouchRewardActor.TRA_Potion_EasyParry,
        "TRA_Potion_HealthM" => eTouchRewardActor.TRA_Potion_HealthM,
        "TRA_Potion_HealthS" => eTouchRewardActor.TRA_Potion_HealthS,
        "TRA_Potion_HealthRegenL" => eTouchRewardActor.TRA_Potion_HealthRegenL,
        "TRA_Potion_DoubleXP" => eTouchRewardActor.TRA_Potion_DoubleXP,
        "TRA_Potion_New5" => eTouchRewardActor.TRA_Potion_New5,
        "TRA_Potion_New6" => eTouchRewardActor.TRA_Potion_New6,
        "TRA_Potion_New7" => eTouchRewardActor.TRA_Potion_New7,
        "TRA_Potion_New8" => eTouchRewardActor.TRA_Potion_New8,
        "TRA_Potion_New" => eTouchRewardActor.TRA_Potion_New9
    };
}
