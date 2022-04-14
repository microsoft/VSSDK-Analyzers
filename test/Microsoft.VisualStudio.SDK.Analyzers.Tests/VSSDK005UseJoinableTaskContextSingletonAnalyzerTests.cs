// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK005UseJoinableTaskContextSingletonAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VSSDK005UseJoinableTaskContextSingletonAnalyzerTests
{
    [Fact]
    public async Task InstantiatingInMethod_ProducesDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Threading;

class Test {
    void Foo() {
        var jtc = new JoinableTaskContext();
    }
}
";
        Microsoft.CodeAnalysis.Testing.DiagnosticResult expected = Verify.Diagnostic().WithSpan(6, 19, 6, 44);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task InstantiatingInField_ProducesDiagnosticAsync()
    {
        var test = @"
using Microsoft.VisualStudio.Threading;

class Test {
    JoinableTaskContext jtc = new JoinableTaskContext();
}
";
        Microsoft.CodeAnalysis.Testing.DiagnosticResult expected = Verify.Diagnostic().WithSpan(5, 31, 5, 56);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task InstantiatingSimilarlyNamedType_ProducesNoDiagnosticAsync()
    {
        var test = @"
class Test {
    JoinableTaskContext jtc = new JoinableTaskContext();
}

class JoinableTaskContext { }
";
        await Verify.VerifyAnalyzerAsync(test);
    }
}
