// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK009UseUIThreadVsTaskRunContextAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VSSDK009UseUIThreadVsTaskRunContextAnalyzerTests
{
    [Fact]
    public async Task StartOnIdleWithBackgroundThreadPriorityProducesDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class Test
{
    void M(JoinableTaskFactory joinableTaskFactory)
    {
        joinableTaskFactory.StartOnIdle(() => { }, {|#0:VsTaskRunContext.BackgroundThread|});
    }
}
";

        await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic().WithLocation(0).WithArguments("StartOnIdle"));
    }

    [Fact]
    public async Task RunAsyncWithBackgroundThreadPriorityProducesDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class Test
{
    void M(JoinableTaskFactory joinableTaskFactory)
    {
        joinableTaskFactory.RunAsync({|#0:VsTaskRunContext.BackgroundThread|}, async () => await Task.Yield());
    }
}
";

        await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic().WithLocation(0).WithArguments("RunAsync"));
    }

    [Fact]
    public async Task WithPriorityWithCurrentContextProducesDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class Test
{
    void M(JoinableTaskFactory joinableTaskFactory)
    {
        joinableTaskFactory.WithPriority({|#0:VsTaskRunContext.CurrentContext|});
    }
}
";

        await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic().WithLocation(0).WithArguments("WithPriority"));
    }

    [Fact]
    public async Task WithPriorityWithUIThreadSendProducesDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class Test
{
    void M(JoinableTaskFactory joinableTaskFactory)
    {
        joinableTaskFactory.WithPriority({|#0:VsTaskRunContext.UIThreadSend|});
    }
}
";

        await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic().WithLocation(0).WithArguments("WithPriority"));
    }

    [Fact]
    public async Task StartOnIdleWithAllowedPriorityProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class Test
{
    void M(JoinableTaskFactory joinableTaskFactory)
    {
        joinableTaskFactory.StartOnIdle(() => { }, VsTaskRunContext.UIThreadBackgroundPriority);
    }
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RunAsyncWithAllowedPriorityProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class Test
{
    void M(JoinableTaskFactory joinableTaskFactory)
    {
        joinableTaskFactory.RunAsync(VsTaskRunContext.UIThreadNormalPriority, async () => await Task.Yield());
    }
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task StaticStartOnIdleWithCurrentContextProducesDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class Test
{
    void M(JoinableTaskFactory joinableTaskFactory)
    {
        VsTaskLibraryHelper.StartOnIdle(joinableTaskFactory, () => { }, {|#0:VsTaskRunContext.CurrentContext|});
    }
}
";

        await Verify.VerifyAnalyzerAsync(test, Verify.Diagnostic().WithLocation(0).WithArguments("StartOnIdle"));
    }

    [Fact]
    public async Task StartOnIdleWithVariablePriorityProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class Test
{
    void M(JoinableTaskFactory joinableTaskFactory, VsTaskRunContext priority)
    {
        joinableTaskFactory.StartOnIdle(() => { }, priority);
    }
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task WithPriorityWithUIThreadPriorityProducesNoDiagnosticAsync()
    {
        var test = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

class Test
{
    void M(JoinableTaskFactory joinableTaskFactory)
    {
        joinableTaskFactory.WithPriority(VsTaskRunContext.UIThreadNormalPriority);
    }
}
";

        await Verify.VerifyAnalyzerAsync(test);
    }
}
