// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.SDK.Analyzers.Tests
{
    using System;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;
    using Xunit.Abstractions;

    public class VSSDK001DeriveFromAsyncPackageTests : DiagnosticVerifier
    {
        private DiagnosticResult expect = new DiagnosticResult
        {
            Id = VSSDK001DeriveFromAsyncPackage.Id,
            SkipVerifyMessage = true,
            Severity = DiagnosticSeverity.Info,
        };

        public VSSDK001DeriveFromAsyncPackageTests(ITestOutputHelper logger)
            : base(logger)
        {
        }

        [Fact]
        public void PackageDerivedClassProducesDiagnostic()
        {
            var test = @"
using Microsoft.VisualStudio.Shell;

class Test : Package {
}
";

            this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 14, 4, 21) };
            this.VerifyCSharpDiagnostic(test, this.expect);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VSSDK001DeriveFromAsyncPackage();
    }
}
