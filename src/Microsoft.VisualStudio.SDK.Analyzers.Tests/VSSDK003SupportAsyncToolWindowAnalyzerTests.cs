// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.SDK.Analyzers;
using Microsoft.VisualStudio.SDK.Analyzers.Tests;
using Xunit;
using Xunit.Abstractions;

public class VSSDK003SupportAsyncToolWindowAnalyzerTests : DiagnosticVerifier
{
    private DiagnosticResult expect = new DiagnosticResult
    {
        Id = VSSDK003SupportAsyncToolWindowAnalyzer.Id,
        SkipVerifyMessage = true,
        Severity = DiagnosticSeverity.Info,
    };

    public VSSDK003SupportAsyncToolWindowAnalyzerTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void SyncToolWindow_NonAsyncPackage_ProducesDiagnostic()
    {
        var package = @"
using Microsoft.VisualStudio.Shell;

[ProvideToolWindow(typeof(ToolWindow1))]
class ToolWindow1Package : Package
{
}
";
        var toolWindow = @"
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

        this.VerifyCSharpDiagnostic(new[] { package, toolWindow });
    }

    [Fact]
    public void SyncToolWindow_AsyncPackage_ProducesDiagnostic()
    {
        var package = @"
using Microsoft.VisualStudio.Shell;

[ProvideToolWindow(typeof(ToolWindow1))]
class ToolWindow1Package : AsyncPackage
{
}
";
        var toolWindow = @"
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

        this.expect.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 27, 4, 38) };
        this.VerifyCSharpDiagnostic(new[] { package, toolWindow }, this.expect);
    }

    [Fact]
    public void AsyncToolWindow_ProducesNoDiagnostic()
    {
        var package = @"
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
        var toolWindow = @"
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

        this.VerifyCSharpDiagnostic(new[] { package, toolWindow });
    }

    [Fact]
    public void RTWCompatibleAsyncToolWindow_ProducesNoDiagnostic()
    {
        var package = @"
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
        var toolWindow = @"
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

        this.VerifyCSharpDiagnostic(new[] { package, toolWindow });
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VSSDK003SupportAsyncToolWindowAnalyzer();
}
