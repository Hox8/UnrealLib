using UnrealLib.Core;
using UnrealLib.Experimental.UnObj.DefaultProperties;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Infinity_Blade;

public partial class ClashMobRewardData : PropertyHolder
{
    [UProperty] public FName SocialChallengeId;
    [UProperty] public int RewardsGiven;
    [UProperty] public eTouchRewardActor RewardType = eTouchRewardActor.TRA_Random;
    [UProperty] public string RewardData;
    [UProperty] public ClashMobRewardState PendingState = ClashMobRewardState.CMRS_NoState;
}
