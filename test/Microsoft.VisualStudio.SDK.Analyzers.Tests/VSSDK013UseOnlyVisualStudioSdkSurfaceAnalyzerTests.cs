// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.Shell;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK013UseOnlyVisualStudioSdkSurfaceAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VSSDK013UseOnlyVisualStudioSdkSurfaceAnalyzerTests
{
    [Test]
    public async Task APIOutsideSurfaceProducesDiagnosticAsync()
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using Microsoft.VisualStudio.Shell;

                class Test
                {
                    Package? Package;
                }
                """,
        };
        AddSurface(test, "Microsoft.VisualStudio.Shell.Interop");
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(new DiagnosticDescriptor(
                VSSDK013UseOnlyVisualStudioSdkSurfaceAnalyzer.Id,
                "Use APIs in the Visual Studio SDK surface",
                "Visual Studio API '{0}' is in assembly '{1}', which is not included in the referenced SDK package surface",
                "Reliability",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true))
                .WithSpan(5, 5, 5, 12)
                .WithArguments("Microsoft.VisualStudio.Shell.Package", typeof(Package).Assembly.GetName().Name));

        await test.RunAsync();
    }

    [Test]
    public async Task APIInSurfaceProducesNoDiagnosticAsync()
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using Microsoft.VisualStudio.Shell;

                class Test
                {
                    Package? Package;
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);

        await test.RunAsync();
    }

    [Test]
    public async Task MissingSurfaceProducesConfigurationDiagnosticAsync()
    {
        var test = new Verify.Test(includeVisualStudioSdk: false)
        {
            TestCode = "class Test { }",
        };
        test.ExpectedDiagnostics.Add(new DiagnosticResult(new DiagnosticDescriptor(
            VSSDK013UseOnlyVisualStudioSdkSurfaceAnalyzer.MissingSurfaceId,
            "Provide the Visual Studio SDK surface",
            "No non-empty Visual Studio SDK surface AdditionalFile was provided with VsSdkSurface metadata",
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true)).WithNoLocation());

        await test.RunAsync();
    }

    [Test]
    public async Task ReflectionTypeNameOutsideSurfaceProducesDiagnosticAsync()
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using System;

                class Test
                {
                    Type? Type = Type.GetType({|#0:"Microsoft.VisualStudio.FakeType, Microsoft.VisualStudio.Private"|});
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);
        test.ExpectedDiagnostics.Add(ReflectionTypeNameDiagnostic()
            .WithLocation(0)
            .WithArguments("Microsoft.VisualStudio.FakeType, Microsoft.VisualStudio.Private"));

        await test.RunAsync();
    }

    [Test]
    public async Task ReflectionTypeNameInSurfaceProducesNoDiagnosticAsync()
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using System;

                class Test
                {
                    Type? Type = Type.GetType("Microsoft.VisualStudio.Shell.Package");
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);

        await test.RunAsync();
    }

    [Test]
    public async Task AssemblyLoadOutsideSurfaceProducesDiagnosticAsync()
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using System.Reflection;

                class Test
                {
                    Assembly? Assembly = Assembly.Load({|#0:"Microsoft.VisualStudio.Private"|});
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);
        test.ExpectedDiagnostics.Add(ReflectionTypeNameDiagnostic()
            .WithLocation(0)
            .WithArguments("Microsoft.VisualStudio.Private"));

        await test.RunAsync();
    }

    [Test]
    public async Task ReflectionMemberLookupOnSdkTypeProducesDiagnosticAsync()
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using Microsoft.VisualStudio.Shell;

                class Test
                {
                    object? Member = {|#0:typeof(Package).GetMethod("PrivateMethod")|};
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);
        test.ExpectedDiagnostics.Add(ReflectionMemberLookupDiagnostic()
            .WithLocation(0)
            .WithArguments("Microsoft.VisualStudio.Shell.Package"));

        await test.RunAsync();
    }

    [Test]
    public async Task NonPublicBindingFlagsOnSdkTypeProducesDiagnosticAsync()
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using System.Reflection;
                using Microsoft.VisualStudio.Shell;

                class Test
                {
                    object? Members = {|#0:typeof(Package).GetMembers(BindingFlags.NonPublic | BindingFlags.Instance)|};
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);
        test.ExpectedDiagnostics.Add(ReflectionMemberLookupDiagnostic()
            .WithLocation(0)
            .WithArguments("Microsoft.VisualStudio.Shell.Package"));

        await test.RunAsync();
    }

    private static void AddSurface(Verify.Test test, string content)
    {
        test.TestState.AdditionalFiles.Add(("Microsoft.VisualStudio.SDK.surface.txt", SourceText.From(content)));
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", SourceText.From(
            """
            is_global = true
            build_metadata.AdditionalFiles.VsSdkSurface = Microsoft.VisualStudio.SDK
            """)));
    }

    private static DiagnosticResult ReflectionTypeNameDiagnostic()
    {
        return new DiagnosticResult(new DiagnosticDescriptor(
            VSSDK013UseOnlyVisualStudioSdkSurfaceAnalyzer.ReflectionTypeNameId,
            "Use reflection with APIs in the Visual Studio SDK surface",
            "Reflection string '{0}' names an assembly or type outside the referenced SDK package surface",
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true));
    }

    private static DiagnosticResult ReflectionMemberLookupDiagnostic()
    {
        return new DiagnosticResult(new DiagnosticDescriptor(
            VSSDK013UseOnlyVisualStudioSdkSurfaceAnalyzer.ReflectionMemberLookupId,
            "Avoid reflection-based member lookup on Visual Studio SDK types",
            "Reflection-based member lookup on Visual Studio type '{0}' can break when the SDK changes",
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true));
    }
}
