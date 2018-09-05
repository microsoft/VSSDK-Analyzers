// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.VisualStudio.Shell;

public class CSharpCodeFixTest<TAnalyzer, TCodeFix> : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
     where TAnalyzer : DiagnosticAnalyzer, new()
     where TCodeFix : CodeFixProvider, new()
{
    private static readonly MetadataReference PresentationFrameworkReference = MetadataReference.CreateFromFile(typeof(UserControl).Assembly.Location);
    private static readonly MetadataReference MPFReference = MetadataReference.CreateFromFile(typeof(Package).Assembly.Location);

    private static readonly ImmutableArray<string> VSSDKPackageReferences = ImmutableArray.Create(new string[]
    {
        Path.Combine("Microsoft.VisualStudio.OLE.Interop", "7.10.6071", "lib", "Microsoft.VisualStudio.OLE.Interop.dll"),
        Path.Combine("Microsoft.VisualStudio.Shell.Interop", "7.10.6072", "lib\\net11", "Microsoft.VisualStudio.Shell.Interop.dll"),
        Path.Combine("Microsoft.VisualStudio.Shell.Interop.8.0", "8.0.50728", "lib\\net11", "Microsoft.VisualStudio.Shell.Interop.8.0.dll"),
        Path.Combine("Microsoft.VisualStudio.Shell.Interop.9.0", "9.0.30730", "lib\\net11", "Microsoft.VisualStudio.Shell.Interop.9.0.dll"),
        Path.Combine("Microsoft.VisualStudio.Shell.Interop.10.0", "10.0.30320", "lib\\net20", "Microsoft.VisualStudio.Shell.Interop.10.0.dll"),
        Path.Combine("Microsoft.VisualStudio.Shell.Interop.11.0", "11.0.61031", "lib\\net20", "Microsoft.VisualStudio.Shell.Interop.11.0.dll"),
        Path.Combine("Microsoft.VisualStudio.Shell.Interop.14.0", "14.3.26929", "lib\\net20", "Microsoft.VisualStudio.Shell.Interop.14.0.DesignTime.dll"),
        Path.Combine("Microsoft.VisualStudio.Shell.Interop.15.3.DesignTime", "15.0.26929", "lib\\net20", "Microsoft.VisualStudio.Shell.Interop.15.3.DesignTime.dll"),
        Path.Combine("Microsoft.VisualStudio.Shell.Interop.15.6.DesignTime", "15.6.27415", "lib\\net20", "Microsoft.VisualStudio.Shell.Interop.15.6.DesignTime.dll"),
        Path.Combine("Microsoft.VisualStudio.Shell.15.0", "15.6.27415", "lib\\net45", "Microsoft.VisualStudio.Shell.15.0.dll"),
        Path.Combine("Microsoft.VisualStudio.Shell.Framework", "15.6.27415", "lib\\net45", "Microsoft.VisualStudio.Shell.Framework.dll"),
        Path.Combine("Microsoft.VisualStudio.Threading", "15.8.122", "lib\\net45", "Microsoft.VisualStudio.Threading.dll"),
        Path.Combine("Microsoft.VisualStudio.Validation", "15.3.15", "lib\\net45", "Microsoft.VisualStudio.Validation.dll"),
    });

    public CSharpCodeFixTest()
    {
        this.SolutionTransforms.Add((solution, projectId) =>
        {
            solution = solution.AddMetadataReference(projectId, PresentationFrameworkReference)
                .AddMetadataReference(projectId, MPFReference);

            if (this.IncludeVisualStudioSdk)
            {
                var nugetPackagesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
                foreach (var reference in VSSDKPackageReferences)
                {
                    solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(Path.Combine(nugetPackagesFolder, reference)));
                }
            }

            return solution;
        });
    }

    public bool IncludeVisualStudioSdk { get; set; } = true;
}
