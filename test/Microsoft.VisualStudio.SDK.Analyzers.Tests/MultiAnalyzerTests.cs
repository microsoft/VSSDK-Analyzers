// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.SDK.Analyzers;
using Xunit;

public class MultiAnalyzerTests
{
    [Fact]
    public async Task BasicPackageAsync()
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

        await new CSharpTest
        {
            TestCode = test,
        }.RunAsync();
    }

    [Fact]
    public async Task NoVSSDKAsync()
    {
        var test = @"
using System;
using System.Threading;

class Test {
}
";

        await new CSharpTest(includeVisualStudioSdk: false)
        {
            TestCode = test,
        }.RunAsync();
    }

    internal class CSharpTest : CSharpCodeFixVerifier<VSSDK001DeriveFromAsyncPackageAnalyzer, EmptyCodeFixProvider>.Test
    {
        internal CSharpTest(bool includeVisualStudioSdk = true)
            : base(includeVisualStudioSdk)
        {
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            return from type in typeof(VSSDK001DeriveFromAsyncPackageAnalyzer).Assembly.GetTypes()
                   where type.GetCustomAttributes(typeof(DiagnosticAnalyzerAttribute), true).Any()
                   select (DiagnosticAnalyzer)Activator.CreateInstance(type);
        }
    }
}
