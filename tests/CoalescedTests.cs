using Microsoft.VisualStudio.TestPlatform.Utilities;
using UnrealLib.Config.Coalesced;
using UnrealLib.Enums;
using Xunit.Abstractions;

namespace Tests;

public class CoalescedTests
{
    [Theory]
    [InlineData(@"..\..\..\Files\Coalesced_IB1.bin")]
    [InlineData(@"..\..\..\Files\Coalesced_IB2.bin")]
    [InlineData(@"..\..\..\Files\Coalesced_IB3.bin")]
    [InlineData(@"..\..\..\Files\Coalesced_VOTE.bin")]
    public void Save_File_To_Folder_And_Back_To_File(string coalescedPath)
    {
        // Open Coalesced file
        var coalesced = new Coalesced(coalescedPath);
        coalesced.Load();

        Assert.Equivalent(coalesced.HasError, false);
        Assert.True(coalesced.Inis.Count > 0);

        // Save Coalesced file to folder
        string folderPath = Path.ChangeExtension(coalescedPath, null);
        coalesced.SaveFolder(folderPath);

        int initialIniCount = coalesced.Inis.Count;
        Game initialGame = coalesced.Game;

        // Open the newly-created Coalesced folder
        coalesced = new Coalesced(folderPath);
        coalesced.LoadFolder();

        Assert.Equivalent(coalesced.HasError, false);

        // Check we've output the same number of files to the folder as we have inis in the Coalesced
        // -1 because we use IBHelper file for folder exports
        Assert.Equivalent(Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).Length - 1, initialIniCount);
        Assert.Equivalent((byte)coalesced.Game, (byte)initialGame);

        // Clean up
        Directory.Delete(folderPath, true);
    }

    // Test calling LoadFolder() from a file archive and vice-versa
}
