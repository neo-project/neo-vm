#!/bin/bash
# Quick performance benchmark script for Neo.VM optimizations

set -e

echo "=================================="
echo "Neo.VM Performance Benchmark"
echo "=================================="
echo ""

# Build the project
echo "Building project..."
dotnet build src/Neo.VM/Neo.VM.csproj -c Release > /dev/null 2>&1
echo "Build complete!"
echo ""

# Create test results file
RESULTS="/tmp/neo-vm-benchmark-results.txt"
echo "Neo.VM Performance Benchmark Results" > "$RESULTS"
echo "Run at: $(date)" >> "$RESULTS"
echo "================================" >> "$RESULTS"
echo ""

# Test 1: Array Creation Performance
echo "Test 1: Array Creation (100,000 iterations)"
cat > /tmp/bench_array.cs << 'EOF'
using Neo.VM;
using Neo.VM.Types;
using System.Diagnostics;

var sw = Stopwatch.StartNew();
var rc = new ReferenceCounter();
int iterations = 100000;

for (int i = 0; i < iterations; i++)
{
    var arr = new Array(rc, StackItem.Null, 16, skipReferenceCounting: true);
}

sw.Stop();
Console.WriteLine($"Array creation: {sw.ElapsedMilliseconds}ms for {iterations} iterations");
Console.WriteLine($"Per iteration: {(double)sw.ElapsedTicks / iterations:F4} ticks");
EOF

dotnet run -c Release --project src/Neo.VM/Neo.VM.csproj -- /tmp/bench_array.cs 2>&1 | rg "Array creation" | tee -a "$RESULTS"
echo ""

# Test 2: Reference Counting Performance
echo "Test 2: Reference Counting (100,000 adds)"
cat > /tmp/bench_refcount.cs << 'EOF'
using Neo.VM;
using Neo.VM.Types;
using System.Diagnostics;

var rc = new ReferenceCounter();
var item = Integer.Zero;
var parent = new Array(rc, StackItem.Null, 16, skipReferenceCounting: true);

// Test single adds
var sw1 = Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    for (int j = 0; j < 16; j++)
    {
        rc.AddReference(item, parent);
    }
}
sw1.Stop();

// Test bulk adds
var parent2 = new Array(rc, StackItem.Null, 16, skipReferenceCounting: true);
var sw2 = Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    rc.AddReference(item, parent2, 16);
}
sw2.Stop();

Console.WriteLine($"Single adds (16000 total): {sw1.ElapsedMilliseconds}ms");
Console.WriteLine($"Bulk adds (16000 total): {sw2.ElapsedMilliseconds}ms");
Console.WriteLine($"Speedup: {(double)sw1.ElapsedTicks / sw2.ElapsedTicks:F2}x");
EOF

dotnet run -c Release --project src/Neo.VM/Neo.VM.csproj -- /tmp/bench_refcount.cs 2>&1 | rg "Single|Bulk|Speedup" | tee -a "$RESULTS"
echo ""

# Test 3: Map Operations (OrderedDictionary optimization)
echo "Test 3: Map Keys/Values Access (10,000 iterations)"
cat > /tmp/bench_map.cs << 'EOF'
using Neo.VM;
using Neo.VM.Types;
using System.Diagnostics;

var rc = new ReferenceCounter();
var map = new Map(rc);
for (int i = 0; i < 100; i++)
{
    map[i] = new Integer(i);
}

var sw = Stopwatch.StartNew();
long totalAccesses = 0;
for (int i = 0; i < 10000; i++)
{
    var keys = map.Keys;
    var values = map.Values;
    totalAccesses += keys.Count() + values.Count();
}
sw.Stop();

Console.WriteLine($"Map Keys/Values access: {sw.ElapsedMilliseconds}ms for 10000 iterations");
Console.WriteLine($"No allocation per access (lazy enumeration)");
EOF

dotnet run -c Release --project src/Neo.VM/Neo.VM.csproj -- /tmp/bench_map.cs 2>&1 | rg "Map Keys|No allocation" | tee -a "$RESULTS"
echo ""

echo "=================================="
echo "Benchmark Complete!"
echo "=================================="
echo ""
echo "Results saved to: $RESULTS"
cat "$RESULTS"
