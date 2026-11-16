// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.Shell;

public class MultiAnalyzerTests
{
    [Test]
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

    [Test]
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
            ExpectedDiagnostics =
            {
                new DiagnosticResult(new DiagnosticDescriptor(
                    VSSDK013UseOnlyVisualStudioSdkSurfaceAnalyzer.MissingSurfaceId,
                    "Provide the Visual Studio SDK surface",
                    "No non-empty Visual Studio SDK surface AdditionalFile was provided with VsSdkSurface metadata",
                    "Reliability",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true)).WithNoLocation(),
            },
        }.RunAsync();
    }

    internal class CSharpTest : CSharpCodeFixVerifier<VSSDK001DeriveFromAsyncPackageAnalyzer, EmptyCodeFixProvider>.Test
    {
        internal CSharpTest(bool includeVisualStudioSdk = true)
            : base(includeVisualStudioSdk)
        {
            if (includeVisualStudioSdk)
            {
                this.TestState.AdditionalFiles.Add((
                    "Microsoft.VisualStudio.SDK.surface.txt",
                    SourceText.From(string.Join(
                        Environment.NewLine,
                        new[]
                        {
                            typeof(AsyncPackage).Assembly.GetName().Name,
                            typeof(ServiceProgressData).Assembly.GetName().Name,
                        }.Distinct(StringComparer.OrdinalIgnoreCase)))));
                this.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", SourceText.From(
                    """
                    is_global = true
                    build_metadata.AdditionalFiles.VsSdkSurface = Microsoft.VisualStudio.SDK
                    """)));
            }
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            return from type in typeof(VSSDK001DeriveFromAsyncPackageAnalyzer).Assembly.GetTypes()
                   where type.GetCustomAttributes(typeof(DiagnosticAnalyzerAttribute), true).Any()
                   select (DiagnosticAnalyzer)Activator.CreateInstance(type);
        }
    }
}
