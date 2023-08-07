using UnLib.Interfaces;

namespace UnLib.Coalesced;

// @TODO: Add API methods for managing coalesced data in-memory.
// E.g. Add/remove section, update property...

// Currently properties are stored as a list of dumb data. I may want to change this in future.
public class Property : ISerializable
{
    private string Key;
    private string Value;

    public void Serialize(UnrealStream unStream)
    {
        unStream.Serialize(ref Key);
        unStream.Serialize(ref Value);
    }

    public static Property GetProperty(string line)
    {
        for (var i = 0; i < line.Length; i++)
            if (line[i] == '=')
                return new Property
                {
                    Key = line[..i],
                    Value = line[(i + 1)..]
                };

        return new Property
        {
            Key = line,
            Value = string.Empty
        };
    }
}

// Current approach does not interpret data; it simply stores a list of string key/value pairs.
public class Section : ISerializable
{
    public List<Property> Properties;

    public void Serialize(UnrealStream unStream)
    {
        unStream.Serialize(ref Properties);
    }

    public void UpdateProperty(string line)
    {
        var prop = Property.GetProperty(line);

        Properties.Add(prop);
    }
}

public class Ini : ISerializable
{
    public Dictionary<string, Section> Sections;

    public void Serialize(UnrealStream uStream)
    {
        uStream.Serialize(ref Sections);
    }
}