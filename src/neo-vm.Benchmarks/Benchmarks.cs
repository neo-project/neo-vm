using System.Diagnostics;

namespace Neo.VM
{
    public static class Benchmarks
    {
        public static void Tanya4()
        {
            // L01: INITSLOT 1, 0
            // L02: NEWARRAY0
            // L03: DUP
            // L04: DUP
            // L05: PUSHINT16 2043
            // L06: STLOC 0
            // L07: PUSH1
            // L08: PACK
            // L09: LDLOC 0
            // L10: DEC
            // L11: STLOC 0
            // L12: LDLOC 0
            // L13: JMPIF_L L07
            // L14: PUSH1
            // L15: PACK
            // L16: APPEND
            // L17: PUSHINT32 38000
            // L18: STLOC 0
            // L19: PUSH0
            // L20: PICKITEM
            // L21: LDLOC 0
            // L22: DEC
            // L23: STLOC 0
            // L24: LDLOC 0
            // L25: JMPIF_L L19
            // L26: DROP
            Run(nameof(Tanya4), "VwEAwkpKAfsHdwARwG8AnXcAbwAl9////xHAzwJwlAAAdwAQzm8AnXcAbwAl9////0U=");
        }

        private static void Run(string name, string poc)
        {
            byte[] script = Convert.FromBase64String(poc);
            using ExecutionEngine engine = new();
            engine.LoadScript(script);
            Stopwatch stopwatch = Stopwatch.StartNew();
            engine.Execute();
            stopwatch.Stop();
            Debug.Assert(engine.State == VMState.HALT);
            Console.WriteLine($"Benchmark: {name}, Time: {stopwatch.Elapsed}");
        }
    }
}
