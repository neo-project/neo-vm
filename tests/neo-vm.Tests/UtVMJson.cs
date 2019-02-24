using System.IO;
using System.Text;
using Neo.Test.Extensions;
using Neo.Test.Types;
using Xunit;

namespace Neo.Test
{
    public class UtVMJson : VMJsonTestBase
    {
        [Fact]
        public void TestJson()
        {
            foreach (var file in Directory.GetFiles("./Tests/", "*.json", SearchOption.AllDirectories))
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