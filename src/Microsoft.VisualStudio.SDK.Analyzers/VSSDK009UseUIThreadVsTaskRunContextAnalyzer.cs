// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.SDK.Analyzers;

/// <summary>
/// Reports incorrect <see cref="Types.VsTaskRunContext"/> values passed to selected <see cref="Types.VsTaskLibraryHelper"/> methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class VSSDK009UseUIThreadVsTaskRunContextAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
    /// </summary>
    public const string Id = "VSSDK009";

    /// <summary>
    /// A reusable descriptor for diagnostics produced by this analyzer.
    /// </summary>
    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: Id,
        title: "Use approved VsTaskRunContext values with VsTaskLibraryHelper",
        messageFormat: "Use VsTaskRunContext.UIThreadBackgroundPriority, VsTaskRunContext.UIThreadIdlePriority, or VsTaskRunContext.UIThreadNormalPriority when calling VsTaskLibraryHelper.{0}",
        description: "VsTaskLibraryHelper.StartOnIdle and priority-related VsTaskLibraryHelper helpers should use VsTaskRunContext.UIThreadBackgroundPriority, VsTaskRunContext.UIThreadIdlePriority, or VsTaskRunContext.UIThreadNormalPriority.",
        helpLinkUri: Utils.GetHelpLink(Id),
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly ImmutableArray<DiagnosticDescriptor> ReusableSupportedDiagnostics = ImmutableArray.Create(Descriptor);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ReusableSupportedDiagnostics;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? vsTaskLibraryHelper = start.Compilation.GetTypeByMetadataName(Types.VsTaskLibraryHelper.FullName);
            INamedTypeSymbol? vsTaskRunContext = start.Compilation.GetTypeByMetadataName(Types.VsTaskRunContext.FullName);
            if (vsTaskLibraryHelper is not null && vsTaskRunContext is not null)
            {
                start.RegisterOperationAction(
                    Utils.DebuggableWrapper(ctxt => AnalyzeInvocation(ctxt, vsTaskLibraryHelper, vsTaskRunContext)),
                    OperationKind.Invocation);
            }
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol vsTaskLibraryHelper, INamedTypeSymbol vsTaskRunContext)
    {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol invokedMethod = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
        if (!SymbolEqualityComparer.Default.Equals(invokedMethod.ContainingType, vsTaskLibraryHelper) ||
            !IsTrackedVsTaskLibraryHelperMethod(invokedMethod.Name))
        {
            return;
        }

        IArgumentOperation? priorityArgument = invocation.Arguments.FirstOrDefault(arg => SymbolEqualityComparer.Default.Equals(arg.Parameter?.Type, vsTaskRunContext));
        if (priorityArgument is null || !IsDisallowedVsTaskRunContextValue(priorityArgument.Value, vsTaskRunContext))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptor, priorityArgument.Syntax.GetLocation(), invokedMethod.Name));
    }

    private static bool IsTrackedVsTaskLibraryHelperMethod(string methodName)
    {
        return methodName is Types.VsTaskLibraryHelper.StartOnIdle or Types.VsTaskLibraryHelper.RunAsync or Types.VsTaskLibraryHelper.WithPriority;
    }

    private static bool IsDisallowedVsTaskRunContextValue(IOperation operation, INamedTypeSymbol vsTaskRunContext)
    {
        bool sawVsTaskRunContextField = false;

        foreach (IFieldReferenceOperation fieldReference in operation.DescendantsAndSelf().OfType<IFieldReferenceOperation>())
        {
            if (!SymbolEqualityComparer.Default.Equals(fieldReference.Field.ContainingType, vsTaskRunContext))
            {
                continue;
            }

            sawVsTaskRunContextField = true;
            if (fieldReference.Field.Name is Types.VsTaskRunContext.UIThreadBackgroundPriority or Types.VsTaskRunContext.UIThreadIdlePriority or Types.VsTaskRunContext.UIThreadNormalPriority)
            {
                return false;
            }
        }

        return sawVsTaskRunContextField;
    }
}
