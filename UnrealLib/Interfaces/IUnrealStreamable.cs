namespace UnrealLib.Interfaces;

/// <summary>
/// Implements common operations for classes implementing an UnrealStream.
/// </summary>
public interface IUnrealStreamable
{
    public string FilePath { get; set; }
    public bool InitFailed { get; set; }
    public bool Modified { get; set; }
    // public Stream BaseStream { get; }
    // public int Length { get; }

    public void Init();
    public void Save();
    // public void Dispose();
    // public void Write(byte[] value);
}