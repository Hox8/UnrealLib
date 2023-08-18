using UnrealLib.Interfaces;

namespace UnrealLib.Coalesced;

public class Property : ISerializable
{
    private string? _key;
    private string? _value;

    public void Serialize(UnrealStream unStream)
    {
        unStream.Serialize(ref _key);
        unStream.Serialize(ref _value);
    }

    public static Property GetProperty(string line)
    {
        for (int i = 0; i < line.Length; i++)
            if (line[i] == '=')
                return new Property
                {
                    _key = line[..i],
                    _value = line[(i + 1)..]
                };

        return new Property
        {
            _key = line,
            _value = string.Empty
        };
    }

    public override string ToString() => $"{_key}={_value}";
}

public class Section : ISerializable
{
    public List<Property> Properties;

    public void Serialize(UnrealStream unStream) => unStream.Serialize(ref Properties);
    public void UpdateProperty(string line) => Properties.Add(Property.GetProperty(line));
}

public class Ini : ISerializable
{
    public Dictionary<string, Section> Sections;
    public readonly string ErrorContext = string.Empty;

    // Parameterless constructor for UStream serializer
    public Ini() { }
    
    public Ini(string filePath, bool collectMetadata=false)
    {
        var curSection = new Section();
        
        // Global 'section-less' properties
        Sections = new Dictionary<string, Section>
        {
            { string.Empty, curSection }
        };
        
        if (!File.Exists(filePath))
        {
            ErrorContext = $"'{filePath}' does not exist!";
            return;
        }

        string[] lines = File.ReadAllLines(filePath); 
        
        for (int i = 0; i < lines.Length; i++)
        {
            // Trim whitespace and unescape newlines
            string trimmed = lines[i].Trim().Replace("\\n", "\n"); 
            
            if (trimmed.Length == 0) continue;
            if (trimmed[0] == '[' && trimmed[^1] == ']')
            {
                // Add new section to Sections
                string sectionName = trimmed[1..^1];
                Sections.TryAdd(sectionName, new Section { Properties = new List<Property>() });
                curSection = Sections[sectionName];
                continue;
            }
            
            // @TODO Do property processing here e.g. comment removal
            curSection.UpdateProperty(trimmed);
        }

        // Remove global properties if we don't want them
        if (!collectMetadata) Sections.Remove(string.Empty);
    }

    public void Serialize(UnrealStream uStream)
    {
        uStream.Serialize(ref Sections);
    }
}