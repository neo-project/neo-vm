<a href="https://neo.org/"><img src="https://neo3.azureedge.net/images/logo%20files-dark.svg" width="250px" alt="neo-logo"></a>

# NeoVM — The NEO Virtual Machine

[![NuGet](https://img.shields.io/nuget/v/Neo.VM.svg)](https://www.nuget.org/packages/Neo.VM/)
[![Coverage Status](https://coveralls.io/repos/github/neo-project/neo-vm/badge.svg)](https://coveralls.io/github/neo-project/neo-vm)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

NeoVM is the lightweight, deterministic, stack-based virtual machine that executes Neo smart contracts. It’s designed to be embeddable, predictable, and portable across platforms. For an overview and deep dives (architecture, stacks, instruction set), see the official developer docs. ([NEO Developer Resource](https://developers.neo.org/docs/n3/foundation/neovm))

---

## Features

* **Deterministic & Turing-complete** execution for smart contracts.
* **Small, embeddable runtime** suitable for host applications beyond the Neo blockchain.
* **Clear isolation boundary**: external effects are provided by the host via an interop/syscall layer (e.g., ApplicationEngine in Neo).
* **Rich instruction set** (control flow, stacks, arithmetic, crypto, data structures).

---

## Packages

The VM is published as a NuGet package:

```
dotnet add package Neo.VM
```

This adds the VM to your project; you can then embed and drive it from your host application.

Targets **.NET 10.0** and **.NET Standard 2.1** (compatible with a wide range of runtimes).

---

## Contributing

Contributions are welcome! Typical flow:

1. Fork the repo and create a feature branch.
2. Make changes with tests (`tests/Neo.VM.Tests`).
3. Ensure `dotnet test` passes and follow standard C# conventions.
4. Open a pull request with a clear description and rationale.

---

## See also

* **neo (core library)** — base classes, ledger, P2P, IO. ([Github](https://github.com/neo-project/neo))
* **neo-devpack-dotnet** — C# → NeoVM compiler and developer toolkit. ([Github](https://github.com/neo-project/neo-devpack-dotnet))
