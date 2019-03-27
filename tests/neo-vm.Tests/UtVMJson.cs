using System.IO;
using System.Text;
using Neo.Test.Extensions;
using Neo.Test.Types;
using Xunit;

namespace Neo.Test
{
    public class UtVMJson : VMJsonTestBase
    {
        [Theory]
        [InlineData("./Tests/Others")]
        [InlineData("./Tests/OpCodes/Arrays")]
        [InlineData("./Tests/OpCodes/Stack")]
        [InlineData("./Tests/OpCodes/Splice")]
        [InlineData("./Tests/OpCodes/Control")]
        [InlineData("./Tests/OpCodes/Push")]
        [InlineData("./Tests/OpCodes/Numeric")]
        [InlineData("./Tests/OpCodes/Exceptions")]
        public void TestJson(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories))
            {
                var json = File.ReadAllText(file, Encoding.UTF8);
                var ut = json.DeserializeJson<VMUT>();

                if (ut.Name != Path.GetFileNameWithoutExtension(file))
                {
                    // Add filename

                    ut.Name += $" [{Path.GetFileNameWithoutExtension(file)}]";
                }

                ExecuteTest(ut);
            }
        }
    }
}