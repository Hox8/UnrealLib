using System;
using UnrealLib.Experimental.UnObj.DefaultProperties;

namespace UnrealLib.Experimental.Infinity_Blade.SaveData_Classes;

public class PersistGameOptions : PropertyHolder
{
    public bool bInvertLook;
    public bool bDisableTutorial;
    public bool bDisableSoundFX;
    public float DodgeScale = -1.0f;
    public float DodgePos;
    public float SoundFXScale = 1.0f;
    public SocialChallengeSave[]? SocialChallengeSaveEvents;

    internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
    {
        switch (tag.Name.GetString)
        {
            case nameof(bInvertLook): Ar.Serialize(ref bInvertLook); break;
            case nameof(bDisableTutorial): Ar.Serialize(ref bDisableTutorial); break;
            case nameof(bDisableSoundFX): Ar.Serialize(ref bDisableSoundFX); break;
            case nameof(DodgeScale): Ar.Serialize(ref DodgeScale); break;
            case nameof(DodgePos): Ar.Serialize(ref DodgePos); break;
            case nameof(SoundFXScale): Ar.Serialize(ref SoundFXScale); break;
            case nameof(SocialChallengeSaveEvents): throw new NotImplementedException(); // SocialChallengeSaveEvents[tag.ArrayIndex].SerializeProperties(Ar); break;
            default: Ar.Position += tag.Size; break;
        }
    }
}

public class SocialChallengeSave : PropertyHolder
{
    public string EventID;
    public bool bPurge;
    public bool bHasPreRegistered;
    public bool bSeenAvail;
    public bool bSeenReward;
    public bool bIgnoreExpired;
    public bool bRemoveFromAll;
    public string[]? GiftedTo;
    public string[]? GiftedFrom;

    internal override void ParseProperty(UnrealArchive Ar, FPropertyTag tag)
    {
        switch (tag.Name.GetString)
        {
            case nameof(EventID): Ar.Serialize(ref EventID); break;
            case nameof(bPurge): Ar.Serialize(ref bPurge); break;
            case nameof(bHasPreRegistered): Ar.Serialize(ref bHasPreRegistered); break;
            case nameof(bSeenAvail): Ar.Serialize(ref bSeenAvail); break;
            case nameof(bSeenReward): Ar.Serialize(ref bSeenReward); break;
            case nameof(bIgnoreExpired): Ar.Serialize(ref bIgnoreExpired); break;
            case nameof(bRemoveFromAll): Ar.Serialize(ref bRemoveFromAll); break;
            case nameof(GiftedTo): throw new NotImplementedException();// Ar.Serialize(ref GiftedTo); break;
            case nameof(GiftedFrom): throw new NotImplementedException();//Ar.Serialize(ref GiftedFrom); break;
            default: base.ParseProperty(Ar, tag); break;
        }
    }
}