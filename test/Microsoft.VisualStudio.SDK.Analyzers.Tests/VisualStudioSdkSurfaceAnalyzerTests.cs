// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.Shell;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VisualStudioSdkSurfaceAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VisualStudioSdkSurfaceAnalyzerTests
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
                    {|#0:Package|}? Package;
                }
                """,
        };
        AddSurface(test, "Microsoft.VisualStudio.Shell.Interop");
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(new DiagnosticDescriptor(
                VisualStudioSdkSurfaceAnalyzer.Id,
                "Avoid APIs outside the Visual Studio SDK surface",
                "Avoid Visual Studio API '{0}' from assembly '{1}', which is outside the referenced SDK package surface",
                "Reliability",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true))
                .WithLocation(0)
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
    [Arguments(null)]
    [Arguments("")]
    [Arguments("\n# No assemblies\n")]
    public async Task MissingOrEmptySurfaceProducesNoDiagnosticAsync(string? surface)
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using System;
                using System.Reflection;
                using Microsoft.VisualStudio.Shell;

                class Test
                {
                    Package? Package;
                    Type? Type = Type.GetType("Microsoft.VisualStudio.PrivateType, Microsoft.VisualStudio.Private");
                    Assembly? Assembly = Assembly.Load("Microsoft.VisualStudio.Private");
                    object? Members = typeof(Package).GetMembers(BindingFlags.NonPublic);
                }
                """,
        };
        if (surface is not null)
        {
            AddSurface(test, surface);
        }

        await test.RunAsync();
    }

    [Test]
    public async Task UntaggedSurfaceProducesNoDiagnosticAsync()
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
        test.TestState.AdditionalFiles.Add(("Microsoft.VisualStudio.SDK.surface.txt", SourceText.From("Microsoft.VisualStudio.Shell.Interop")));

        await test.RunAsync();
    }

    [Test]
    [Arguments("Type.GetType", "Microsoft.VisualStudio.FakeType, Microsoft.VisualStudio.Private")]
    [Arguments("typeof(Test).Assembly.GetType", "Microsoft.VisualStudio.FakeType, Microsoft.VisualStudio.Private")]
    [Arguments("Type.GetType", "System.Collections.Generic.List`1[[Microsoft.VisualStudio.FakeType, Microsoft.VisualStudio.Private]], mscorlib")]
    [Arguments("Type.GetType", "Microsoft.VisualStudio.Container`1[[Microsoft.VisualStudio.FakeType, Microsoft.VisualStudio.Private]], SdkAssembly")]
    [Arguments("Type.GetType", "Microsoft.VisualStudio.Container`1[[Microsoft.VisualStudio.Shell.Package, SdkAssembly]], Microsoft.VisualStudio.Private")]
    [Arguments("typeof(Test).Assembly.GetType", "System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Collections.Generic.List`1[[Microsoft.VisualStudio.FakeType, Microsoft.VisualStudio.Private, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], mscorlib]], mscorlib")]
    public async Task ReflectionTypeNameOutsideSurfaceProducesDiagnosticAsync(string lookup, string typeName)
    {
        typeName = typeName.Replace("SdkAssembly", typeof(Package).Assembly.GetName().Name!);
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ $$"""
                using System;

                class Test
                {
                    Type? Type = {{lookup}}({|#0:"{{typeName}}"|});
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);
        test.ExpectedDiagnostics.Add(ReflectionDiagnostic()
            .WithLocation(0)
            .WithArguments(typeName));

        await test.RunAsync();
    }

    [Test]
    [Arguments("Microsoft.VisualStudio.Shell.Package")]
    [Arguments("Microsoft.VisualStudio.Shell.Package, SdkAssembly, Version=17.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [Arguments("System.Collections.Generic.List`1[[Microsoft.VisualStudio.Shell.Package, SdkAssembly, Version=17.0.0.0, Culture=neutral, PublicKeyToken=null]], mscorlib")]
    [Arguments("System.Collections.Generic.Dictionary`2[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[Microsoft.VisualStudio.Shell.Package, SdkAssembly]], mscorlib")]
    [Arguments("Microsoft.VisualStudio.DoesNotExist")]
    [Arguments("System.Collections.Generic.List`1[Microsoft.VisualStudio.DoesNotExist]")]
    public async Task SupportedOrUnresolvedReflectionTypeNameProducesNoDiagnosticAsync(string typeName)
    {
        typeName = typeName.Replace("SdkAssembly", typeof(Package).Assembly.GetName().Name!);
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ $$"""
                using System;

                class Test
                {
                    Type? Type = Type.GetType("{{typeName}}");
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);

        await test.RunAsync();
    }

    [Test]
    [Arguments("Microsoft.VisualStudio.Shell.Package")]
    [Arguments("System.Collections.Generic.List`1[Microsoft.VisualStudio.Shell.Package]")]
    public async Task ResolvedUnqualifiedTypeOutsideSurfaceProducesDiagnosticAsync(string typeName)
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ $$"""
                using System;

                class Test
                {
                    Type? Type = Type.GetType({|#0:"{{typeName}}"|});
                }
                """,
        };
        AddSurface(test, "Microsoft.VisualStudio.Shell.Interop");
        test.ExpectedDiagnostics.Add(ReflectionDiagnostic()
            .WithLocation(0)
            .WithArguments(typeName));

        await test.RunAsync();
    }

    [Test]
    [Arguments("Load", "Microsoft.VisualStudio.Private")]
    [Arguments("Load", "Microsoft.VisualStudio.Private, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [Arguments("LoadFrom", @"C:\VisualStudio\Microsoft.VisualStudio.Private.dll")]
    public async Task AssemblyLoadOutsideSurfaceProducesDiagnosticAsync(string method, string assemblyName)
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ $$"""
                using System.Reflection;

                class Test
                {
                    Assembly? Assembly = Assembly.{{method}}({|#0:@"{{assemblyName}}"|});
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);
        test.ExpectedDiagnostics.Add(ReflectionDiagnostic()
            .WithLocation(0)
            .WithArguments(assemblyName));

        await test.RunAsync();
    }

    [Test]
    public async Task NamedNonPublicMemberLookupsOnSdkTypeProduceDiagnosticsAsync()
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using System.Reflection;
                using Microsoft.VisualStudio.Shell;

                class Test
                {
                    const BindingFlags PrivateMembers = BindingFlags.NonPublic | BindingFlags.Instance;
                    object? Method = {|#0:typeof(Package).GetMethod("PrivateMethod", PrivateMembers)|};
                    object? Property = {|#1:typeof(Package).GetProperty("PrivateProperty", PrivateMembers)|};
                    object? Field = {|#2:typeof(Package).GetField("PrivateField", PrivateMembers)|};
                    object? Member = {|#3:typeof(Package).GetMember("PrivateMethod", MemberTypes.Method, PrivateMembers)|};
                    object? Event = {|#4:typeof(Package).GetEvent("PrivateEvent", PrivateMembers)|};
                    object? Events = {|#5:typeof(Package).GetEvents(PrivateMembers)|};
                    object? NestedType = {|#6:typeof(Package).GetNestedType("PrivateType", PrivateMembers)|};
                    object? NestedTypes = {|#7:typeof(Package).GetNestedTypes(PrivateMembers)|};
                    object? FilteredMembers = {|#8:typeof(Package).FindMembers(MemberTypes.All, PrivateMembers, null, null)|};
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);
        for (int i = 0; i < 9; i++)
        {
            test.ExpectedDiagnostics.Add(ReflectionDiagnostic()
                .WithLocation(i)
                .WithArguments("Microsoft.VisualStudio.Shell.Package"));
        }

        await test.RunAsync();
    }

    [Test]
    public async Task PublicReflectionMemberLookupsOnSdkTypeProduceNoDiagnosticAsync()
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using System.Reflection;
                using Microsoft.VisualStudio.Shell;

                class Test
                {
                    void Lookup()
                    {
                        typeof(Package).GetMethod("ToString");
                        typeof(Package).GetMethod("PrivateMethod");
                        typeof(Package).GetProperty("Property");
                        typeof(Package).GetField("Field");
                        typeof(Package).GetMember("Member");
                        typeof(Package).GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance);
                        typeof(Package).GetProperty("Property", BindingFlags.Public | BindingFlags.Instance);
                        typeof(Package).GetField("Field", BindingFlags.Public | BindingFlags.Instance);
                        typeof(Package).GetMember("Member", MemberTypes.TypeInfo, BindingFlags.Public | BindingFlags.Instance);
                        typeof(Package).GetEvent("Event");
                        typeof(Package).GetEvents();
                        typeof(Package).GetNestedType("NestedType");
                        typeof(Package).GetNestedTypes();
                        typeof(Package).GetEvent("Event", BindingFlags.Public);
                        typeof(Package).GetEvents(BindingFlags.Public);
                        typeof(Package).GetNestedType("NestedType", BindingFlags.Public);
                        typeof(Package).GetNestedTypes(BindingFlags.Public);
                        typeof(Package).FindMembers(MemberTypes.All, BindingFlags.Public, null, null);
                    }
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);

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
        test.ExpectedDiagnostics.Add(ReflectionDiagnostic()
            .WithLocation(0)
            .WithArguments("Microsoft.VisualStudio.Shell.Package"));

        await test.RunAsync();
    }

    [Test]
    [Arguments("Package[]")]
    [Arguments("Package[,]")]
    [Arguments("Package[][]")]
    public async Task ArrayReflectionDoesNotLookUpElementMembersAsync(string typeName)
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ $$"""
                using System.Reflection;
                using Microsoft.VisualStudio.Shell;

                class Test
                {
                    object? Members = typeof({{typeName}}).GetMembers(BindingFlags.NonPublic | BindingFlags.Instance);
                    object? Events = typeof({{typeName}}).GetEvents(BindingFlags.NonPublic);
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);

        await test.RunAsync();
    }

    [Test]
    public async Task NonConstantBindingFlagsProduceNoDiagnosticAsync()
    {
        var test = new Verify.Test
        {
            TestCode = /* lang=c#-test */ """
                using System.Reflection;
                using Microsoft.VisualStudio.Shell;

                class Test
                {
                    void Lookup(BindingFlags flags)
                    {
                        typeof(Package).GetEvent("Event", flags);
                        typeof(Package).GetEvents(flags);
                        typeof(Package).GetNestedType("NestedType", flags);
                        typeof(Package).GetNestedTypes(flags);
                        typeof(Package).FindMembers(MemberTypes.All, flags, null, null);
                    }
                }
                """,
        };
        AddSurface(test, typeof(Package).Assembly.GetName().Name!);

        await test.RunAsync();
    }

    private static void AddSurface(Verify.Test test, string content)
    {
        test.TestState.AdditionalFiles.Add(("/Microsoft.VisualStudio.SDK.surface.txt", SourceText.From(content)));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", SourceText.From(
            """
            root = true
            [Microsoft.VisualStudio.SDK.surface.txt]
            build_metadata.AdditionalFiles.VsSdkSurface = Microsoft.VisualStudio.SDK
            """)));
    }

    private static DiagnosticResult ReflectionDiagnostic()
    {
        return new DiagnosticResult(new DiagnosticDescriptor(
            VisualStudioSdkSurfaceAnalyzer.ReflectionTypeNameId,
            "Avoid unsupported reflection on Visual Studio APIs",
            "Avoid unsupported reflection on Visual Studio API '{0}'",
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true));
    }
}
