// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.SDK.Analyzers;

/// <summary>
/// Discovers services provided by an <c>AsyncPackage</c> without asynchronous query support.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VSSDK011ProvideServiceAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
    /// </summary>
    public const string Id = "VSSDK011";

    /// <summary>
    /// A reusable descriptor for diagnostics produced by this analyzer.
    /// </summary>
    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: Id,
        title: "Provide services asynchronously from AsyncPackage",
        messageFormat: "Services provided by an AsyncPackage must be asynchronously queryable and registered with a Task-returning service factory",
        helpLinkUri: Utils.GetHelpLink(Id),
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.RegisterCompilationStartAction(context =>
        {
            INamedTypeSymbol? asyncPackageType = context.Compilation.GetTypeByMetadataName(Types.AsyncPackage.FullName);
            if (asyncPackageType is null)
            {
                return;
            }

            INamedTypeSymbol? provideServiceAttributeType = context.Compilation.GetTypeByMetadataName(Types.ProvideServiceAttribute.FullName);
            if (provideServiceAttributeType is not null)
            {
                context.RegisterSymbolAction(
                    Utils.DebuggableWrapper((SymbolAnalysisContext context) => AnalyzeNamedType(context, asyncPackageType, provideServiceAttributeType)),
                    SymbolKind.NamedType);
            }

            INamedTypeSymbol? serviceContainerType = context.Compilation.GetTypeByMetadataName(Types.IServiceContainer.FullName);
            INamedTypeSymbol? serviceCreatorCallbackType = context.Compilation.GetTypeByMetadataName(Types.ServiceCreatorCallback.FullName);
            if (serviceContainerType is not null && serviceCreatorCallbackType is not null)
            {
                context.RegisterOperationAction(
                    Utils.DebuggableWrapper((OperationAnalysisContext context) => AnalyzeInvocation(context, asyncPackageType, serviceContainerType, serviceCreatorCallbackType)),
                    OperationKind.Invocation);
            }
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol asyncPackageType, INamedTypeSymbol provideServiceAttributeType)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;
        if (!Utils.IsEqualToOrDerivedFrom(namedType.BaseType, asyncPackageType))
        {
            return;
        }

        foreach (AttributeData attribute in namedType.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, provideServiceAttributeType)))
        {
            bool isAsyncQueryable = attribute.NamedArguments.FirstOrDefault(a => a.Key == Types.ProvideServiceAttribute.IsAsyncQueryable).Value.Value as bool? ?? false;
            if (isAsyncQueryable || attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) is not AttributeSyntax attributeSyntax)
            {
                continue;
            }

            AttributeArgumentSyntax? isAsyncQueryableArgument = attributeSyntax.ArgumentList?.Arguments
                .FirstOrDefault(a => a.NameEquals?.Name.Identifier.ValueText == Types.ProvideServiceAttribute.IsAsyncQueryable);
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, isAsyncQueryableArgument?.GetLocation() ?? attributeSyntax.GetLocation()));
        }
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol asyncPackageType,
        INamedTypeSymbol serviceContainerType,
        INamedTypeSymbol serviceCreatorCallbackType)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name != Types.IServiceContainer.AddService ||
            !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, serviceContainerType) ||
            invocation.TargetMethod.Parameters.Length < 2 ||
            !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.Parameters[1].Type, serviceCreatorCallbackType) ||
            !Utils.IsEqualToOrDerivedFrom(context.ContainingSymbol.ContainingType, asyncPackageType) ||
            !IsContainingTypeInstance(invocation.Instance))
        {
            return;
        }

        IArgumentOperation? serviceFactoryArgument = invocation.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == 1);
        context.ReportDiagnostic(Diagnostic.Create(Descriptor, serviceFactoryArgument?.Syntax.GetLocation() ?? invocation.Syntax.GetLocation()));
    }

    private static bool IsContainingTypeInstance(IOperation? operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance };
    }
}
