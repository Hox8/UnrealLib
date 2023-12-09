namespace UnrealLib.Interfaces;

public interface ISerializable
{
    // Various Serialize() calls expect an UnrealPackage, but I haven't figured out a good way to support both.
    // @TODO: Until I figure out a good method, various Serialize() calls will cast UnrealArchive to UnrealPackage
    // Debug assertions will be in place
    public void Serialize(UnrealArchive Ar);
}
