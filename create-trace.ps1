$now = [DateTime]::now
$day = $now.ToString("yyyyMMdd")
$time = $now.ToString("HHmmss");
$trackAll = "Neo.VM.TrackAll_$($day)_$($time).nettrace"
$trackCompound = "Neo.VM.TrackCompound_$($day)_$($time).nettrace"

dotnet clean
if (-not $?) { exit }
dotnet build -c Release
if (-not $?) { exit }
dotnet trace collect --providers Neo.VM -o ./$trackAll -- benchmarks\Neo.VM.Benchmarks\bin\Release\net6.0\Neo.VM.Benchmarks.exe 
if (-not $?) { exit }

dotnet clean
if (-not $?) { exit }
dotnet build -c Release /p:DefineConstants=TRACK_COMPOUND_ONLY
if (-not $?) { exit }
dotnet trace collect --providers Neo.VM -o ./$trackCompound -- benchmarks\Neo.VM.Benchmarks\bin\Release\net6.0\Neo.VM.Benchmarks.exe 
if (-not $?) { exit }

benchmarks\Neo.VM.TraceReader\bin\Release\net6.0\Neo.VM.TraceReader.exe .\$trackAll
if (-not $?) { exit }
benchmarks\Neo.VM.TraceReader\bin\Release\net6.0\Neo.VM.TraceReader.exe .\$trackCompound
