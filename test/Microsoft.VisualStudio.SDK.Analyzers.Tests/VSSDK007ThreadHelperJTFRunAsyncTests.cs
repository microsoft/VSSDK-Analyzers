// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = CSharpCodeFixVerifier<
    Microsoft.VisualStudio.SDK.Analyzers.VSSDK007ThreadHelperJTFRunAsync,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class VSSDK007ThreadHelperJTFRunAsyncTests
{
    [Fact]
    public async Task RunAsync_Dropped_Warning()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
namespace ConsoleApplication1
{
    class Test
    {
        static void Foo()
        {
            ThreadHelper.JoinableTaskFactory.{|#0:RunAsync|}(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
        }
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task RunAsync_Forget_Warning()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
namespace ConsoleApplication1
{
    class Test
    {
        static void Foo()
        {
            ThreadHelper.JoinableTaskFactory.{|#0:RunAsync|}(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }).Task.Forget();
        }
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task RunAsync_FileAndForget_Warning()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
namespace ConsoleApplication1
{
    class Test
    {
        static void Foo()
        {
            ThreadHelper.JoinableTaskFactory.{|#0:RunAsync|}(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }).FileAndForget(""test"");
        }
    }
}";

        DiagnosticResult expected = Verify.Diagnostic().WithLocation(0);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task RunAsync_Awaited_NoWarning()
    {
        var test = @"
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
namespace ConsoleApplication1
{
    class Test
    {
        static async Task FooAsync()
        {
            await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
        }
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RunAsync_Joined_NoWarning()
    {
        var test = @"
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
namespace ConsoleApplication1
{
    class Test
    {
        static async Task FooAsync()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }).Join();
        }
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RunAsync_AssignedButNotJoined_Warning()
    {
        var test = @"
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;
namespace ConsoleApplication1
{
    class Test
    {
        private static JoinableTask _task;
        static async Task FooAsync()
        {
            var task1 = ThreadHelper.JoinableTaskFactory.{|#0:RunAsync|}(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            JoinableTask task2;
            task2 = ThreadHelper.JoinableTaskFactory.{|#1:RunAsync|}(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            var task3 = ThreadHelper.JoinableTaskFactory.{|#2:RunAsync|}(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            task3.JoinAsync();  // Note: missing await
            var task4 = ThreadHelper.JoinableTaskFactory.{|#3:RunAsync|}(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            task4.FileAndForget(""test"");
            _task = ThreadHelper.JoinableTaskFactory.{|#4:RunAsync|}(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            var task5 = ThreadHelper.JoinableTaskFactory.{|#5:RunAsync|}(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            ForwardButNotJoined(task5);
            var task6 = ThreadHelper.JoinableTaskFactory.{|#6:RunAsync|}(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            OnlyFirstParameterIsJoined(null, task6);
            ForwardButNotJoined(
                ThreadHelper.JoinableTaskFactory.{|#7:RunAsync|}(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                }));
        }
        static void ForwardButNotJoined(JoinableTask joinableTask)
        {
            
        }
        static void OnlyFirstParameterIsJoined(JoinableTask jt1, JoinableTask jt2)
        {
            jt1.Join();
        }
    }
}";

        DiagnosticResult[] expected = new DiagnosticResult[8]
        {
                Verify.Diagnostic().WithLocation(0),
                Verify.Diagnostic().WithLocation(1),
                Verify.Diagnostic().WithLocation(2),
                Verify.Diagnostic().WithLocation(3),
                Verify.Diagnostic().WithLocation(4),
                Verify.Diagnostic().WithLocation(5),
                Verify.Diagnostic().WithLocation(6),
                Verify.Diagnostic().WithLocation(7),
        };

        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task RunAsync_AssignedAndJoined_NoWarning()
    {
        var test = @"
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
namespace ConsoleApplication1
{
    class Test
    {
        static async Task FooAsync()
        {
            var task1 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            await task1;

            var task2 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            task2.Join();

            var task3 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            await task3.JoinAsync();

            // Strange but legal:
            await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }).JoinAsync();
        }
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RunAsync_JoinedElsewhere_NoWarning()
    {
        var test = @"
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;
namespace ConsoleApplication1
{
    class Test
    {
        static async Task FooAsync()
        {
            var task1 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            DoJoin(task1);
            var task2 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            DoAwait(task2);
            var task3 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            ForwardAndJoin(task3);
            var task4 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            ForwardAndAwait(task4);
            JoinableTask task5;
            task5 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            DoJoin(task5);
            var task6 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            await DoAwaitAsync(task6);
            /*** Test forward and join two tasks. ***/
            var task7 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            var task8 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            DoJoinTwo(task7, task8);
            /*** Test argument is tracked to the correct parameter ***/
            var task9 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>           
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            DoJoinTwo(task9, null);
            var task10 = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
            DoJoinTwo(null, task10);
        }
        static void DoJoin(JoinableTask joinableTask)
        {
            joinableTask.Join();
        }
        static void DoJoinTwo(JoinableTask jt1, JoinableTask jt2)
        {
            if (jt1 != null) jt1.Join();
            if (jt2 != null) jt2.Join();
        }
        static void DoAwait(JoinableTask joinableTask)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await joinableTask;
            });
        }
        static async Task DoAwaitAsync(JoinableTask joinableTask)
        {
            await joinableTask;
        }
        static void ForwardAndJoin(JoinableTask jt1)
        {
            Forward2(jt1);
        }
        static void ForwardAndAwait(JoinableTask jt1)
        {
            DoAwait(jt1);
        }
        static void Forward2(JoinableTask jt2)
        {
            Forward3(jt2);
        }
        static void Forward3(JoinableTask jt3)
        {
            DoJoin(jt3);
        }
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RunAsync_Dropped_NotThreadHelper_NoWarning()
    {
        var test = @"
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
namespace ConsoleApplication1
{
    class MyThreadHelper
    {
        private readonly JoinableTaskContext ctx;
        private readonly JoinableTaskCollection collection;

        public MyThreadHelper()
        {
            ctx = new JoinableTaskContext();
            collection = ctx.CreateCollection();
            JoinableTaskFactory = ctx.CreateFactory(collection);
        }

        public JoinableTaskFactory JoinableTaskFactory { get; }
    }

    class Test
    {
        static void Foo()
        {
            MyThreadHelper myHelper = new MyThreadHelper();
            myHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            });
        }
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RunAsync_DifferentType_NoWarning()
    {
        var test = @"
using System.Threading.Tasks;
namespace ConsoleApplication1
{
    class JoinableTaskFactory
    {
        public async Task RunAsync()
        {
            await Task.Delay(1);
        }
    }

    static class ThreadHelper
    {
        public static JoinableTaskFactory JoinableTaskFactory { get; set; }
    }

    class Test
    {
        static void Foo()
        {
            ThreadHelper.JoinableTaskFactory = new JoinableTaskFactory();
            ThreadHelper.JoinableTaskFactory.RunAsync();

            var jtf = new JoinableTaskFactory();
            jtf.RunAsync();
        }
    }
}";

        await Verify.VerifyAnalyzerAsync(test);
    }
}
