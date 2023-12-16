using UnrealLib.Core;

namespace Tests;

public class FNameTests
{
    #region Name splitting

    [Fact]
    public void Split_Empty_Name()
    {
        FName.SplitName("", out var name, out int number);

        Assert.Equivalent(name, "");
        Assert.Equivalent(number, 0);
    }

    [Fact]
    public void Split_Non_Delimited_Name()
    {
        FName.SplitName("String", out var name, out int number);

        Assert.Equivalent(name, "String");
        Assert.Equivalent(number, 0);
    }

    [Fact]
    public void Split_Delimited_Name()
    {
        FName.SplitName("String_3", out var name, out int number);

        Assert.Equivalent(name, "String");
        Assert.Equivalent(number, 3);
    }

    [Fact]
    public void Split_Delimited_Zero_Name()
    {
        FName.SplitName("String_0", out var name, out int number);

        Assert.Equivalent(name, "String");
        Assert.Equivalent(number, 0);
    }

    // Padded names don't count as numbers.
    [Fact]
    public void Split_Delimited_Padded_Name()
    {
        FName.SplitName("String_03", out var name, out int number);

        Assert.Equivalent(name, "String_03");
        Assert.Equivalent(number, 0);
    }

    #endregion

    #region Name serialization

    #endregion
}
