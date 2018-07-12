// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.SDK.Analyzers.Tests;
using Xunit;
using Xunit.Abstractions;

public class MultiAnalyzerTests : DiagnosticVerifier
{
    public MultiAnalyzerTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void BasicPackage()
    {
        var test = @"
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

class Test : AsyncPackage {
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
        await base.InitializeAsync(cancellationToken, progress);
    }
}
";

        this.VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoVSSDK()
    {
        var test = @"
using System;
using System.Threading;

class Test {
}
";

        this.VerifyCSharpDiagnostic(test, vssdk: false);
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => throw new NotImplementedException();

    protected override ImmutableArray<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
    {
        IEnumerable<DiagnosticAnalyzer> analyzers = from type in typeof(VSSDK001DeriveFromAsyncPackageAnalyzer).Assembly.GetTypes()
                                                    where type.GetCustomAttributes(typeof(DiagnosticAnalyzerAttribute), true).Any()
                                                    select (DiagnosticAnalyzer)Activator.CreateInstance(type);
        return analyzers.ToImmutableArray();
    }
}
