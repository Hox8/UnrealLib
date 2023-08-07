namespace UnLib.Interfaces;

/// Classes implementing this interface are able to be read from and written to an UnrealStream.
public interface ISerializable
{
    public void Serialize(UnrealStream unStream);
}