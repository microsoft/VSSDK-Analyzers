// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using System.Reflection;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic()
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();

    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => new DiagnosticResult(descriptor);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public static Task VerifyCodeFixAsync(string source, string fixedSource)
        => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
        => VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
    {
        var test = new Test
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        if (fixedSource == source)
        {
            test.FixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
            test.FixedState.MarkupHandling = MarkupMode.Allow;
            test.BatchFixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
            test.BatchFixedState.MarkupHandling = MarkupMode.Allow;
        }

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        private static readonly MetadataReference PresentationFrameworkReference = MetadataReference.CreateFromFile(typeof(System.Windows.Controls.UserControl).Assembly.Location);
        private static readonly MetadataReference MPFReference = MetadataReference.CreateFromFile(typeof(Microsoft.VisualStudio.Shell.Package).Assembly.Location);

        private static readonly string NuGetConfigPath = FindNuGetConfigPath();

        private static readonly ReferenceAssemblies DefaultReferences = ReferenceAssemblies.NetFramework.Net472.Wpf
            .WithNuGetConfigFilePath(NuGetConfigPath);

        private static readonly ReferenceAssemblies VsSdkReferences = DefaultReferences
            .AddPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.VisualStudio.Shell.15.0", "17.12.40392")));

        static Test()
        {
            // Force all test runners to talk the latest TLS version so nuget.org packages can be downloaded.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public Test(bool includeVisualStudioSdk = true)
        {
            this.ReferenceAssemblies = includeVisualStudioSdk ? VsSdkReferences : DefaultReferences;

            const string additionalFilePrefix = "AdditionalFiles.";
            this.TestState.AdditionalFiles.AddRange(
                from resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames()
                where resourceName.StartsWith(additionalFilePrefix, StringComparison.Ordinal)
                let content = ReadManifestResource(Assembly.GetExecutingAssembly(), resourceName)
                select (filename: resourceName.Substring(additionalFilePrefix.Length), SourceText.From(content)));
        }

        private static string ReadManifestResource(Assembly assembly, string resourceName)
        {
            using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName) ?? throw Assumes.Fail("Resource not found."));
            return reader.ReadToEnd();
        }

        private static string FindNuGetConfigPath()
        {
            string? path = AppContext.BaseDirectory;
            while (path is not null)
            {
                string candidate = Path.Combine(path, "nuget.config");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                path = Path.GetDirectoryName(path);
            }

            throw new FileNotFoundException("Could not find NuGet.config by searching up from " + AppContext.BaseDirectory);
        }
    }
}
