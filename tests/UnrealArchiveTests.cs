using UnrealLib;

namespace Tests
{
    public class UnrealArchiveTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("C:\\C:\\File.txt")]
        [InlineData("7123")]
        [InlineData("D:\\File>.txt")]
        public void Test_Against_Invalid_Pathnames(string path)
        {
            var Ar = new UnrealArchive(path);
            Assert.Equivalent(Ar.HasError, true);
        }
    }
}
