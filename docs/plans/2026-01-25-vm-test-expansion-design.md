# VM Test Expansion Design

## Goal
Increase VM correctness coverage using unit tests only. Keep all existing JSON-based VM tests running in the fast suite, add C# unit tests for core invariants, and introduce deterministic randomized tests in a nightly suite.

## Non-goals
- No benchmarks or performance tests.
- No external test dependencies (MSTest only).
- No changes to VM runtime behavior.

## Constraints
- JSON tests must continue to run in fast CI.
- Tests must be deterministic and reproducible.
- Nightly tests are opt-in via filter, not required for local fast runs.

## Architecture Overview
The test strategy is layered:
- **Fast suite (CI):** Full JSON test corpus + new unit tests for invariants + execute-vs-debugger equivalence on curated scripts.
- **Nightly suite:** Deterministic randomized script tests with bounded resources, plus extended equivalence coverage.

## Components

### 1) Core invariants (fast)
Add new MSTest classes:
- `UT_ExecutionEngine_Invariants`: state transitions, empty invocation behavior, instruction pointer progression.
- `UT_StackAndSlots_Limits`: stack depth and slot boundary validation using public APIs.
- `UT_ExceptionPaths`: FAULT propagation and exception messages for THROW/ASSERT/ABORT patterns.

### 2) JSON tests (fast)
Keep `UT_VMJson` as-is. Add new JSON files only to fill opcode gaps or missing edge cases. JSON remains the baseline behavioral spec.

### 3) Execute vs Debugger equivalence (fast)
Add `UT_ExecuteVsDebugger` that runs a curated set of scripts using both:
- `ExecutionEngine.Execute()`
- `Debugger.StepInto()` loop until HALT or FAULT

Assert equality for:
- VM state
- Result stack
- Invocation stack (IP, next opcode, slots)

### 4) Deterministic randomized tests (nightly)
Add `UT_Fuzz_Scripts` with `[TestCategory("Nightly")]`:
- Fixed RNG seed per test.
- Script length and stack bounds enforced.
- Safe opcode subset only (no external syscall unless stubbed).
- Execute-vs-debugger equivalence check for each generated script.

## Execution Flow
1) Build script bytes via `ScriptBuilder` or from JSON.
2) Run either engine mode to completion (HALT/FAULT or max step bound).
3) Capture normalized snapshots of state and stacks for assertions.

## Error Handling and Safety
- Hard caps: max instructions, max script length, max stack depth.
- If bounds exceeded, fail with a minimal repro (script hex + opcode list).
- For FAULT paths, assert `VMState.FAULT` and stable exception message when applicable.

## Test Matrix
**Fast (CI):**
- All JSON tests
- Core invariants
- Execute-vs-debugger equivalence (curated scripts)

**Nightly:**
- Deterministic randomized scripts
- Extended equivalence coverage

## CI Usage
- Fast: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj`
- Nightly: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter TestCategory=Nightly`

## Implementation Notes
- New helpers go in `tests/Neo.VM.Tests/Helpers/` to avoid duplication.
- Use ASCII-only text and avoid modifying existing JSON files unless adding coverage.
