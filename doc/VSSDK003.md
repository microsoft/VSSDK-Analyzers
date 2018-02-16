# VSSDK003 Support async tool windows

Offer an async tool window factory for your tool windows so Visual Studio startup
is fast if the tool window is initially visible, and remains responsive when your
tool window is activated later in the session.

This analyzer only flags synchronous tool windows when proffered from an `AsyncPackage`-derived
VS package and when targeting Visual Studio 2017 Update 6 or later.

## Examples of patterns that are flagged by this analyzer

```csharp
[Guid("bd7b3e0c-f79e-46c1-8a04-12cbb8161ce5")]
public class ToolWindow1 : ToolWindowPane
{
    public ToolWindow1() : base(null)
    {
        this.Caption = "ToolWindow1";
        this.Content = new ToolWindow1Control();
    }
}

public class ToolWindow1Package : AsyncPackage
{
    // A lack of async tool window factory overrides
}
```

## Solution

Override the async tool window factory methods on your `AsyncPackage`-derived class
and offer a constructor overload in your `ToolWindowPane`-derived class that accepts one parameter.

```csharp
[Guid("bd7b3e0c-f79e-46c1-8a04-12cbb8161ce5")]
public class ToolWindow1 : ToolWindowPane
{
    public ToolWindow1() : base(null)
    {
        this.Caption = "ToolWindow1";
        this.Content = new ToolWindow1Control();
    }

    public ToolWindow1(string message)
        : this()
    {
    }
}

public class ToolWindow1Package : AsyncPackage
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
            return "ToolWindow1 loading";
        }

        return base.GetToolWindowTitle(toolWindowType, id);
    }

    protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
    {
        // potentially expensive work, preferably done on a background thread where possible.
        await Task.Delay(5000, cancellationToken);

        return "foo"; // this is passed to the tool window constructor
    }
}
```
