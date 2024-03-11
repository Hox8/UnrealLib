using UnrealLib.Experimental.UnObj.DefaultProperties;
using UnrealLib.UProperty;

namespace UnrealLib.Experimental.Infinity_Blade.SaveData_Classes;

public partial class PersistGameOptions : PropertyHolder
{
    [UProperty] public bool bInvertLook;
    [UProperty] public bool bDisableTutorial;
    [UProperty] public bool bDisableSoundFX;
    [UProperty] public float DodgeScale = -1.0f;
    [UProperty] public float DodgePos;
    [UProperty] public float SoundFXScale = 1.0f;
    [UProperty] public SocialChallengeSave[]? SocialChallengeSaveEvents;
}

public partial class SocialChallengeSave : PropertyHolder
{
    [UProperty] public string EventID;
    [UProperty] public bool bPurge;
    [UProperty] public bool bHasPreRegistered;
    [UProperty] public bool bSeenAvail;
    [UProperty] public bool bSeenReward;
    [UProperty] public bool bIgnoreExpired;
    [UProperty] public bool bRemoveFromAll;
    [UProperty] public string[]? GiftedTo;
    [UProperty] public string[]? GiftedFrom;
}
