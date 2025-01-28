// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK003SupportAsyncToolWindowAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VSSDK003SupportAsyncToolWindowAnalyzerTests
{
    [Fact]
    public async Task SyncToolWindow_NonAsyncPackage_ProducesDiagnosticAsync()
    {
        var package = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideToolWindow(typeof(ToolWindow1))]
class ToolWindow1Package : Package
{
}
";
        var toolWindow = /* lang=c#-test */ @"
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

[Guid(""bd7b3e0c-f79e-46c1-8a04-12cbb8161ce5"")]
public class ToolWindow1 : ToolWindowPane
{
    public ToolWindow1() : base(null)
    {
        this.Caption = ""ToolWindow1"";
        this.Content = new UserControl();
    }
}
";

        await new Verify.Test
        {
            TestState =
            {
                Sources = { package, toolWindow },
            },
        }.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SyncToolWindow_AsyncPackage_ProducesDiagnosticAsync()
    {
        var package = /* lang=c#-test */ @"
using Microsoft.VisualStudio.Shell;

[ProvideToolWindow(typeof(ToolWindow1))]
class ToolWindow1Package : AsyncPackage
{
}
";
        var toolWindow = /* lang=c#-test */ @"
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

[Guid(""bd7b3e0c-f79e-46c1-8a04-12cbb8161ce5"")]
public class ToolWindow1 : ToolWindowPane
{
    public ToolWindow1() : base(null)
    {
        this.Caption = ""ToolWindow1"";
        this.Content = new UserControl();
    }
}
";

        await new Verify.Test
        {
            TestState =
            {
                Sources = { package, toolWindow },
            },
            ExpectedDiagnostics = { Verify.Diagnostic().WithSpan(4, 27, 4, 38) },
        }.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AsyncToolWindow_ProducesNoDiagnosticAsync()
    {
        var package = /* lang=c#-test */ @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

[ProvideToolWindow(typeof(ToolWindow1))]
class ToolWindow1Package : AsyncPackage
{
    public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
    {
        if (toolWindowType == typeof(ToolWindow1).GUID)
        {
            return this;
        }

        return base.GetAsyncToolWindowFactory(toolWindowType);
    }

    protected override string GetToolWindowTitle(Type toolWindowType, int id)
    {
        if (toolWindowType == typeof(ToolWindow1))
        {
            return ""ToolWindow1 loading"";
        }

        return base.GetToolWindowTitle(toolWindowType, id);
    }

    protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
    {
        await Task.Delay(5000);

        return ""foo"";
    }
}
";
        var toolWindow = /* lang=c#-test */ @"
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

[Guid(""bd7b3e0c-f79e-46c1-8a04-12cbb8161ce5"")]
public class ToolWindow1 : ToolWindowPane
{
    public ToolWindow1() : base(null)
    {
        this.Caption = ""ToolWindow1"";
        this.Content = new UserControl();
    }

    public ToolWindow1(string message)
        : this()
    {
    }
}
";

        await new Verify.Test
        {
            TestState =
            {
                Sources = { package, toolWindow },
            },
        }.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RTWCompatibleAsyncToolWindow_ProducesNoDiagnosticAsync()
    {
        var package = /* lang=c#-test */ @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

[ProvideToolWindow(typeof(ToolWindow1))]
class ToolWindow1Package : AsyncPackage
    , IVsAsyncToolWindowFactory
    , IVsAsyncToolWindowFactoryProvider
{
    IVsTask IVsAsyncToolWindowFactory.InitializeToolWindowAsync(Guid guid, uint id)
    {
        IVsTask task = this.JoinableTaskFactory.RunAsyncAsVsTask(
            VsTaskRunContext.UIThreadBackgroundPriority,
            async (cancellationToken) => await InitializeToolWindowAsync(typeof(ToolWindow1), (int)id, cancellationToken));

        return task;
    }

    void IVsAsyncToolWindowFactory.CreateToolWindow(Guid guid, uint id, object context)
    {
    }

    string IVsAsyncToolWindowFactory.GetToolWindowTitle(Guid guid, uint id)
    {
        throw new NotImplementedException();
    }

    async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);

        return ""foo"";
    }
}
";
        var toolWindow = /* lang=c#-test */ @"
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

[Guid(""bd7b3e0c-f79e-46c1-8a04-12cbb8161ce5"")]
public class ToolWindow1 : ToolWindowPane
{
    public ToolWindow1() : base(null)
    {
        this.Caption = ""ToolWindow1"";
        this.Content = new UserControl();
    }

    public ToolWindow1(string message)
        : this()
    {
    }
}
";

        await new Verify.Test
        {
            TestState =
            {
                Sources = { package, toolWindow },
            },
        }.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NoPackageAttributesAsync()
    {
        var package = /* lang=c#-test */ @"
public class ProtocolPackage : Microsoft.VisualStudio.Shell.AsyncPackage
{
}
    ";
        await Verify.VerifyAnalyzerAsync(package);
    }

    [Fact]
    public async Task OverriddenGetAsyncToolWindowFactory_ToolWindowWithParameterlessConstructor_ProducesNoDiagnosticAsync()
    {
        var package = /* lang=c#-test */ @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

public abstract class ToolkitPackage : AsyncPackage
{
    private List<object> _toolWindowProviders;

    public override IVsAsyncToolWindowFactory? GetAsyncToolWindowFactory(Guid toolWindowType)
    {
        return this;
    }

    protected override string GetToolWindowTitle(Type toolWindowType, int id)
    {
        return base.GetToolWindowTitle(toolWindowType, id);
    }

    protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
    {
        return new object();
    }

    protected override WindowPane InstantiateToolWindow(Type toolWindowType, object context)
    {
        return base.InstantiateToolWindow(toolWindowType, ToolWindowCreationContext.Unspecified);
    }
}

[ProvideToolWindow(typeof(ToolWindow1))]
class ToolWindow1Package : ToolkitPackage
{
}
";
        var toolWindow = /* lang=c#-test */ @"
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

[Guid(""bd7b3e0c-f79e-46c1-8a04-12cbb8161ce5"")]
public class ToolWindow1
{
}
";

        await new Verify.Test
        {
            TestState =
            {
                Sources = { package, toolWindow },
            },
        }.RunAsync(TestContext.Current.CancellationToken);
    }
}
