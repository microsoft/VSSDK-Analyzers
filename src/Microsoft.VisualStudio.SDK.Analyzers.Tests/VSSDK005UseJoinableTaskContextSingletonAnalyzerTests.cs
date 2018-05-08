// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.SDK.Analyzers.Tests;
using Xunit;
using Xunit.Abstractions;

public class VSSDK005UseJoinableTaskContextSingletonAnalyzerTests : DiagnosticVerifier
{
    private DiagnosticResult expect = new DiagnosticResult
    {
        Id = VSSDK005UseJoinableTaskContextSingletonAnalyzer.Id,
        SkipVerifyMessage = true,
        Severity = DiagnosticSeverity.Error,
    };

    public VSSDK005UseJoinableTaskContextSingletonAnalyzerTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void InstantiatingInMethod_ProducesDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Threading;

class Test {
    void Foo() {
        var jtc = new JoinableTaskContext();
    }
}
";
        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 19, 6, 44) };
        this.VerifyCSharpDiagnostic(test, this.expect);
    }

    [Fact]
    public void InstantiatingInField_ProducesDiagnostic()
    {
        var test = @"
using Microsoft.VisualStudio.Threading;

class Test {
    JoinableTaskContext jtc = new JoinableTaskContext();
}
";
        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 5, 31, 5, 56) };
        this.VerifyCSharpDiagnostic(test, this.expect);
    }

    [Fact]
    public void InstantiatingSimilarlyNamedType_ProducesNoDiagnostic()
    {
        var test = @"
class Test {
    JoinableTaskContext jtc = new JoinableTaskContext();
}

class JoinableTaskContext { }
";
        this.VerifyCSharpDiagnostic(test);
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VSSDK005UseJoinableTaskContextSingletonAnalyzer();
}
