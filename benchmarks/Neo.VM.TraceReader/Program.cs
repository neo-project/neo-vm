using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;


foreach (var arg in args)
{
    if (Path.GetExtension(arg) != ".nettrace") continue;
    var results = ProcessLog(arg);
    PrintResults(arg, results);
}

static (IReadOnlyList<Execution> instruction, IReadOnlyList<Execution> checkZero) ProcessLog(string path)
{
    // event provider guids are calculated from the provider name
    // This guid corresponds to the provider named "Neo.VM"
    Guid NEO_VM_PROVIDER = Guid.Parse("a9921cdf-1864-564b-ca57-00407b0262c3");

    Neo.VM.OpCode opcode = Neo.VM.OpCode.RET;
    double insStart = -1;
    double czrStart = -1;
    List<Execution> insList = new();
    List<Execution> czrList = new();

    using var traceLog = new TraceLog(TraceLog.CreateFromEventPipeDataFile(path));
    var traceSource = traceLog.Events.GetSource();
    traceSource.AllEvents += e =>
    {
        if (e.ProviderGuid != NEO_VM_PROVIDER) return;

        if (e.Opcode == TraceEventOpcode.Start)
        {
            switch ((int)e.ID)
            {
                case 1:
                    opcode = (Neo.VM.OpCode)e.PayloadByName("opCode");
                    insStart = e.TimeStampRelativeMSec;
                    break;
                case 106:
                    czrStart = e.TimeStampRelativeMSec;
                    break;
            }
        }

        if (e.Opcode == TraceEventOpcode.Stop)
        {
            switch ((int)e.ID)
            {
                case 2:
                    if (insStart >= 0)
                    {
                        insList.Add(new Execution(opcode, insStart, e.TimeStampRelativeMSec));
                    }
                    insStart = -1;
                    break;
                case 107:
                    if (czrStart >= 0)
                    {
                        czrList.Add(new Execution(opcode, czrStart, e.TimeStampRelativeMSec));
                    }
                    czrStart = -1;
                    break;
            }
        }
    };
    traceSource.Process();
    return (insList, czrList);
}

static void PrintResults(string path, (IReadOnlyList<Execution> instruction, IReadOnlyList<Execution> checkZero) tuple)
{
    var (ins, czr) = tuple;
    var i = CalcDuration(ins);
    var z = CalcDuration(czr);
    var pct = z/i * 100;

    Console.WriteLine($"{Path.GetFileName(path)} results");
    Console.WriteLine($"  Counts: {ins.Count} {czr.Count}");
    Console.WriteLine($"  Durations: {i:0.00} {z:0.00}");
    Console.WriteLine($"  CZR %: {pct:0.00}");

    static double CalcDuration(IReadOnlyList<Execution> executions)
        => executions.Aggregate(0.0, (total, exec) => total + exec.Duration);

}

readonly record struct Execution(Neo.VM.OpCode opCode, double start, double end)
{
    public double Duration => end - start;
}
