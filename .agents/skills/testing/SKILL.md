---
name: testing
description: Provides guidance and instructions for writing, running and filtering tests.
user-invocable: false
---

**IMPORTANT**: This repository uses TUnit with Microsoft.Testing.Platform (MTP v2). Traditional `--filter` syntax does NOT work. Use the options below instead.

* The test project is `test\Microsoft.VisualStudio.SDK.Analyzers.Tests`, covering both the analyzer and code-fix projects.
* Tests use TUnit with Microsoft.Testing.Platform (MTP v2), while retaining xUnit assertions. Traditional VSTest `--filter` syntax does NOT work.

## Running Tests

There *may* be unstable tests that are attributed as such.
Running the [dotnet-test-cloud.ps1](../../../tools/dotnet-test-cloud.ps1) script will execute all previously built tests except the unstable ones.
The commands below will *not* automatically exclude unstable tests, but you may do so by adding a parameter to filter out those that set their `TestCategory` property to `FailsInCloudTest`.

**Run all tests** (from the root of the repository):
```bash
dotnet test --no-build -c Release
```

**Exclude tests marked unstable**:
```bash
dotnet test --no-build -c Release -- --treenode-filter '/*/*/*/*[TestCategory!=FailsInCloudTest]'
```

To run the test project, change to its directory first; the commands below then target it.

**Run tests for a specific test project** (within that test project's directory):
```bash
dotnet test --no-build -c Release
```

**Run a single test method**:
```bash
dotnet test --no-build -c Release -- --treenode-filter "/*/*/ClassName/MethodName"
```

**Run all tests in a test class**:
```bash
dotnet test --no-build -c Release -- --treenode-filter "/*/*/ClassName/*"
```

**Run tests with wildcard matching** (supports wildcards at beginning and/or end):
```bash
dotnet test --no-build -c Release -- --treenode-filter "/*/*/*/*Pattern*"
```

**Run tests with a specific property**:
```bash
dotnet test --no-build -c Release -- --treenode-filter "/*/*/*/*[PropertyName=value]"
```

**Run tests for a specific framework only**:
```bash
dotnet test --no-build -c Release --framework net472
```

**List all available tests without running them**:
```bash
dotnet run --no-build -c Release --framework net472 -- --list-tests
```

**Key points about test filtering with TUnit / MTP v2**:
- Options after `--` are passed to the test runner, not to `dotnet test`
- Use `--treenode-filter` to select tests by assembly, namespace, class, method, or property
- Traditional VSTest `--filter` expressions do NOT work
- Wildcards `*` are supported in tree node segments
- See `--help` for query filter language for advanced scenarios
