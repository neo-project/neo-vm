using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.Test.Types;

namespace Neo.Test
{
    [TestClass]
    public class UtVMJson : VMJsonTestBase
    {
        [TestMethod]
        public void TestOthers() => TestJson("./Tests/Others");

        [TestMethod]
        public void TestOpCodesArrays() => TestJson("./Tests/OpCodes/Arrays");

        [TestMethod]
        public void TestOpCodesStack() => TestJson("./Tests/OpCodes/Stack");

        [TestMethod]
        public void TestOpCodesSplice() => TestJson("./Tests/OpCodes/Splice");

        [TestMethod]
        public void TestOpCodesControl() => TestJson("./Tests/OpCodes/Control");

        [TestMethod]
        public void TestOpCodesPush() => TestJson("./Tests/OpCodes/Push");

        [TestMethod]
        public void TestOpCodesNumeric() => TestJson("./Tests/OpCodes/Numeric");

        [TestMethod]
        public void TestOpCodesExceptions() => TestJson("./Tests/OpCodes/Exceptions");

        private void TestJson(string path)
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