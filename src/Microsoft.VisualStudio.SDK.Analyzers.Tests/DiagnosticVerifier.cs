// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using Microsoft.Build.Utilities;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;
    using Microsoft.VisualStudio.Shell;
    using Xunit;
    using Xunit.Abstractions;

    public abstract class DiagnosticVerifier
    {
        private const string CSharpDefaultFileExt = "cs";
        private const string TestProjectName = "TestProject";
        private const string DefaultFilePathPrefix = "Test";

        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(typeof(Debug).Assembly.Location);
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
            Path.Combine("Microsoft.VisualStudio.Threading", "15.8.99-rc", "lib\\net45", "Microsoft.VisualStudio.Threading.dll"),
        });

        protected DiagnosticVerifier(ITestOutputHelper logger)
        {
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected ITestOutputHelper Logger { get; }

        /// <summary>
        /// Given classes in the form of strings, their language, and an IDiagnosticAnlayzer to apply to it, return the diagnostics found in the string after converting it to a document.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source classes are in</param>
        /// <param name="analyzers">The analyzers to be run on the sources</param>
        /// <param name="hasEntrypoint"><c>true</c> to set the compiler in a mode as if it were compiling an exe (as opposed to a dll).</param>
        /// <param name="allowErrors">A value indicating whether to fail the test if there are compiler errors in the code sample.</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        protected static Diagnostic[] GetSortedDiagnostics(string[] sources, string language, ImmutableArray<DiagnosticAnalyzer> analyzers, bool hasEntrypoint, bool allowErrors = false)
        {
            return GetSortedDiagnosticsFromDocuments(analyzers, GetDocuments(sources, language, hasEntrypoint), allowErrors);
        }

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzers">The analyzers to run on the documents</param>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <param name="allowErrors">A value indicating whether to fail the test if there are compiler errors in the code sample.</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        protected static Diagnostic[] GetSortedDiagnosticsFromDocuments(ImmutableArray<DiagnosticAnalyzer> analyzers, Document[] documents, bool allowErrors = false)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var diagnostics = new List<Diagnostic>();
            foreach (var project in projects)
            {
                var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
                var ordinaryDiags = compilation.GetDiagnostics();
                var errorDiags = ordinaryDiags.Where(d => d.Severity == DiagnosticSeverity.Error);
                if (!allowErrors && errorDiags.Any())
                {
                    Assert.False(true, "Compilation errors exist in the test source code, such as:" + Environment.NewLine + errorDiags.First());
                }

                var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
                var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (int i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult();
                            if (tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }

            var results = SortDiagnostics(diagnostics);
            diagnostics.Clear();
            return results;
        }

        /// <summary>
        /// Create a project using the inputted strings as sources.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <param name="hasEntrypoint"><c>true</c> to set the compiler in a mode as if it were compiling an exe (as opposed to a dll).</param>
        /// <returns>A Project created out of the Documents created from the source strings</returns>
        protected static Project CreateProject(string[] sources, string language = LanguageNames.CSharp, bool hasEntrypoint = false)
        {
            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : throw new NotSupportedException();

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, language)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemReference)
                .AddMetadataReference(projectId, PresentationFrameworkReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, MPFReference)
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(hasEntrypoint ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary))
                .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.CSharp6));

            var pathToLibs = ToolLocationHelper.GetPathToStandardLibraries(".NETFramework", "v4.5.1", string.Empty);
            if (!string.IsNullOrEmpty(pathToLibs))
            {
                var facades = Path.Combine(pathToLibs, "Facades");
                if (Directory.Exists(facades))
                {
                    var facadesAssemblies = new List<MetadataReference>();
                    foreach (var path in Directory.EnumerateFiles(facades, "*.dll"))
                    {
                        facadesAssemblies.Add(MetadataReference.CreateFromFile(path));
                    }

                    solution = solution.AddMetadataReferences(projectId, facadesAssemblies);
                }
            }

            string globalPackagesFolder = Environment.GetEnvironmentVariable("NuGetGlobalPackagesFolder");
            string nugetPackageRoot = string.IsNullOrEmpty(globalPackagesFolder)
                ? Path.Combine(
                    Environment.GetEnvironmentVariable("USERPROFILE"),
                    ".nuget",
                    "packages")
                : globalPackagesFolder;
            var vssdkReferences = VSSDKPackageReferences.Select(e =>
                MetadataReference.CreateFromFile(Path.Combine(nugetPackageRoot, e)));
            solution = solution.AddMetadataReferences(projectId, vssdkReferences);

            int count = 0;
            foreach (var source in sources)
            {
                var newFileName = fileNamePrefix + count + "." + fileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }

            return solution.GetProject(projectId);
        }

        /// <summary>
        /// Helper method to format a Diagnostic into an easily readable string
        /// </summary>
        /// <param name="analyzers">The analyzers that this Verifier tests</param>
        /// <param name="diagnostics">The Diagnostics to be formatted</param>
        /// <returns>The Diagnostics formatted as a string</returns>
        protected static string FormatDiagnostics(ImmutableArray<DiagnosticAnalyzer> analyzers, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine(diagnostics[i].ToString());
            }

            return builder.ToString();
        }

        /// <summary>
        /// Create a Document from a string through creating a project that contains it.
        /// </summary>
        /// <param name="source">Classes in the form of a string</param>
        /// <param name="language">The language the source code is in</param>
        /// <param name="hasEntrypoint"><c>true</c> to set the compiler in a mode as if it were compiling an exe (as opposed to a dll).</param>
        /// <returns>A Document created from the source string</returns>
        protected static Document CreateDocument(string source, string language = LanguageNames.CSharp, bool hasEntrypoint = false)
        {
            return CreateProject(new[] { source }, language, hasEntrypoint).Documents.First();
        }

        /// <summary>
        /// Get the CSharp analyzer being tested - to be implemented in non-abstract class
        /// </summary>
        protected abstract DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer();

        /// <summary>
        /// Get the CSharp analyzers being tested - to be implemented in non-abstract class
        /// </summary>
        protected virtual ImmutableArray<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            return ImmutableArray.Create(this.GetCSharpDiagnosticAnalyzer());
        }

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="expected">An array of <see cref="DiagnosticResult"/> that should appear after the analyzer is run on the source</param>
        protected void VerifyCSharpDiagnostic(string source, params DiagnosticResult[] expected)
        {
            this.VerifyDiagnostics(new[] { source }, LanguageNames.CSharp, this.GetCSharpDiagnosticAnalyzers(), allowErrors: false, expected: expected);
        }

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on multiple input strings as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="sources">Classes, each in the form of a string to run the analyzer on</param>
        /// <param name="expected">An array of <see cref="DiagnosticResult"/> that should appear after the analyzer is run on the source</param>
        protected void VerifyCSharpDiagnostic(string[] sources, params DiagnosticResult[] expected)
        {
            this.VerifyDiagnostics(sources, LanguageNames.CSharp, this.GetCSharpDiagnosticAnalyzers(), allowErrors: false, expected: expected);
        }

        protected void LogFileContent(string source)
        {
            using (var sr = new StringReader(source))
            {
                string line;
                int lineNumber = 1;
                while ((line = sr.ReadLine()) != null)
                {
                    this.Logger.WriteLine("{0,2}: {1}", lineNumber++, line);
                }
            }
        }

        /// <summary>
        /// Sort diagnostics by location in source document
        /// </summary>
        /// <param name="diagnostics">The list of Diagnostics to be sorted</param>
        /// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
        private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        /// <summary>
        /// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <param name="hasEntrypoint"><c>true</c> to set the compiler in a mode as if it were compiling an exe (as opposed to a dll).</param>
        /// <returns>An array of Documents produced from the source strings</returns>
        private static Document[] GetDocuments(string[] sources, string language, bool hasEntrypoint)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
            {
                throw new ArgumentException("Unsupported Language");
            }

            for (int i = 0; i < sources.Length; i++)
            {
                string fileName = language == LanguageNames.CSharp ? "Test" + i + ".cs" : "Test" + i + ".vb";
            }

            var project = CreateProject(sources, language, hasEntrypoint);
            var documents = project.Documents.ToArray();

            if (sources.Length != documents.Length)
            {
                throw new SystemException("Amount of sources did not match amount of Documents created");
            }

            return documents;
        }

        /// <summary>
        /// Helper method to VerifyDiagnosticResult that checks the location of a diagnostic and compares it with the location in the expected DiagnosticResult.
        /// </summary>
        /// <param name="analyzers">The analyzers that were being run on the sources</param>
        /// <param name="diagnostic">The diagnostic that was found in the code</param>
        /// <param name="actual">The Location of the Diagnostic found in the code</param>
        /// <param name="expected">The DiagnosticResultLocation that should have been found</param>
        private static void VerifyDiagnosticLocation(ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            var actualSpan = actual.GetLineSpan();

            Assert.True(
                actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                string.Format(
                    "Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                    expected.Path,
                    actualSpan.Path,
                    FormatDiagnostics(analyzers, diagnostic)));

            var actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
            {
                if (actualLinePosition.Line + 1 != expected.Line)
                {
                    Assert.True(
                        false,
                        string.Format(
                            "Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Line,
                            actualLinePosition.Line + 1,
                            FormatDiagnostics(analyzers, diagnostic)));
                }

                if (expected.EndLine > -1 && actualSpan.EndLinePosition.Line + 1 != expected.EndLine)
                {
                    Assert.True(
                        false,
                        string.Format(
                            "Expected diagnostic to end on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.EndLine,
                            actualSpan.EndLinePosition.Line + 1,
                            FormatDiagnostics(analyzers, diagnostic)));
                }
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
            {
                if (actualLinePosition.Character + 1 != expected.Column)
                {
                    Assert.True(
                        false,
                        string.Format(
                            "Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Column,
                            actualLinePosition.Character + 1,
                            FormatDiagnostics(analyzers, diagnostic)));
                }

                if (expected.EndColumn > -1 && actualSpan.EndLinePosition.Character + 1 != expected.EndColumn)
                {
                    Assert.True(
                        false,
                        string.Format(
                            "Expected diagnostic to end at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.EndColumn,
                            actualSpan.EndLinePosition.Character + 1,
                            FormatDiagnostics(analyzers, diagnostic)));
                }
            }
        }

        /// <summary>
        /// General method that gets a collection of actual diagnostics found in the source after the analyzer is run,
        /// then verifies each of them.
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="language">The language of the classes represented by the source strings</param>
        /// <param name="analyzers">The analyzers to be run on the source code</param>
        /// <param name="allowErrors">A value indicating whether to fail the test if there are compiler errors in the code sample.</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        private void VerifyDiagnostics(string[] sources, string language, ImmutableArray<DiagnosticAnalyzer> analyzers, bool allowErrors, params DiagnosticResult[] expected)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                this.Logger.WriteLine("File {0} content:", i + 1);
                this.LogFileContent(sources[i]);
            }

            var diagnostics = GetSortedDiagnostics(sources, language, analyzers, allowErrors);
            this.VerifyDiagnosticResults(diagnostics, analyzers, expected);
        }

        /// <summary>
        /// Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
        /// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
        /// </summary>
        /// <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
        /// <param name="analyzers">The analyzers that were being run on the sources</param>
        /// <param name="expectedResults">Diagnostic Results that should have appeared in the code</param>
        private void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, ImmutableArray<DiagnosticAnalyzer> analyzers, params DiagnosticResult[] expectedResults)
        {
            int expectedCount = expectedResults.Count();
            int actualCount = actualResults.Count();

            string diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzers, actualResults.ToArray()) : "    NONE.";
            this.Logger.WriteLine("Actual diagnostics:\n" + diagnosticsOutput);

            Assert.Equal(expectedCount, actualCount);

            for (int i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i];

                if (expected.Line == -1 && expected.Column == -1)
                {
                    if (actual.Location != Location.None)
                    {
                        Assert.True(
                            false,
                            string.Format(
                                "Expected:\nA project diagnostic with No location\nActual:\n{0}",
                                FormatDiagnostics(analyzers, actual)));
                    }
                }
                else
                {
                    VerifyDiagnosticLocation(analyzers, actual, actual.Location, expected.Locations.First());
                    var additionalLocations = actual.AdditionalLocations.ToArray();

                    if (additionalLocations.Length != expected.Locations.Length - 1)
                    {
                        Assert.True(
                            false,
                            string.Format(
                                "Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
                                expected.Locations.Length - 1,
                                additionalLocations.Length,
                                FormatDiagnostics(analyzers, actual)));
                    }

                    for (int j = 0; j < additionalLocations.Length; ++j)
                    {
                        VerifyDiagnosticLocation(analyzers, actual, additionalLocations[j], expected.Locations[j + 1]);
                    }
                }

                if (actual.Id != expected.Id)
                {
                    Assert.True(
                        false,
                        string.Format(
                            "Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Id,
                            actual.Id,
                            FormatDiagnostics(analyzers, actual)));
                }

                if (actual.Severity != expected.Severity)
                {
                    Assert.True(
                        false,
                        string.Format(
                            "Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Severity,
                            actual.Severity,
                            FormatDiagnostics(analyzers, actual)));
                }

                if (!expected.SkipVerifyMessage && actual.GetMessage() != expected.Message)
                {
                    Assert.True(
                        false,
                        string.Format(
                            "Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Message,
                            actual.GetMessage(),
                            FormatDiagnostics(analyzers, actual)));
                }
            }
        }
    }
}
