using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.Test.Types;
using System.Threading.Tasks;
using System;
using System.Diagnostics;

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
        public void TestOpCodesJumps() => TestJson("./Tests/OpCodes/Jumps");

        [TestMethod]
        [Timeout(3500)]
        public void TestOpCodesJumpsUnlimited()
        {
            //Task.Factory.StartNew(() => TestJson("./Tests/OpCodes/JumpsUnlimited")).Wait(TimeSpan.FromSeconds(3));
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var task1 = Task.Factory.StartNew(() => TestJson("./Tests/OpCodes/JumpsUnlimited"));
            task1.Wait(TimeSpan.FromSeconds(3));
            stopwatch.Stop();
            // should fail on timer basis
            Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 3000);
        }

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