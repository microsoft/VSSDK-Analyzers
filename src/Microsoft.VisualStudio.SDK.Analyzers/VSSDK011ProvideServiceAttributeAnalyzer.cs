// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        messageFormat: "Services provided by an AsyncPackage must be asynchronously queryable and must not use a synchronous service factory",
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
                context.RegisterSymbolStartAction(
                    context =>
                    {
                        var namedType = (INamedTypeSymbol)context.Symbol;
                        if (!Utils.IsEqualToOrDerivedFrom(namedType.BaseType, asyncPackageType))
                        {
                            return;
                        }

                        var state = new ServiceRegistrationAnalysisState(namedType, serviceContainerType, serviceCreatorCallbackType);
                        context.RegisterOperationAction(Utils.DebuggableWrapper(state.AnalyzeAssignment), OperationKind.SimpleAssignment);
                        context.RegisterOperationAction(Utils.DebuggableWrapper(state.AnalyzeInvocation), OperationKind.Invocation);
                        context.RegisterSymbolEndAction(Utils.DebuggableWrapper(state.AnalyzeSymbolEnd));
                    },
                    SymbolKind.NamedType);
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

    private static IOperation GetRootOperation(IOperation operation)
    {
        while (operation.Parent is not null)
        {
            operation = operation.Parent;
        }

        return operation;
    }

    private static bool IsContainingTypeInstance(IOperation? operation, IOperation rootOperation, HashSet<ISymbol> visitedSymbols)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation switch
        {
            IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } => true,
            ILocalReferenceOperation localReference => IsAliasForContainingTypeInstance(localReference.Local, rootOperation, visitedSymbols),
            IFieldReferenceOperation fieldReference => IsAliasForContainingTypeInstance(fieldReference.Field, rootOperation, visitedSymbols),
            _ => false,
        };
    }

    private static bool IsAliasForContainingTypeInstance(ISymbol symbol, IOperation rootOperation, HashSet<ISymbol> visitedSymbols)
    {
        if (!visitedSymbols.Add(symbol))
        {
            return false;
        }

        try
        {
            bool foundAssignment = false;
            foreach (IOperation operation in rootOperation.DescendantsAndSelf())
            {
                IOperation? value = operation switch
                {
                    IVariableDeclaratorOperation declarator when
                        SymbolEqualityComparer.Default.Equals(declarator.Symbol, symbol) => declarator.Initializer?.Value,
                    ISimpleAssignmentOperation assignment when
                        SymbolEqualityComparer.Default.Equals(GetReferencedSymbol(assignment.Target), symbol) => assignment.Value,
                    _ => null,
                };

                if (value is null)
                {
                    continue;
                }

                foundAssignment = true;
                if (!IsContainingTypeInstance(value, rootOperation, visitedSymbols))
                {
                    return false;
                }
            }

            return foundAssignment;
        }
        finally
        {
            visitedSymbols.Remove(symbol);
        }
    }

    private static ISymbol? GetReferencedSymbol(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation switch
        {
            ILocalReferenceOperation localReference => localReference.Local,
            IFieldReferenceOperation fieldReference => fieldReference.Field,
            _ => null,
        };
    }

    private sealed class ServiceRegistrationAnalysisState
    {
        private readonly object syncObject = new();
        private readonly INamedTypeSymbol packageType;
        private readonly INamedTypeSymbol serviceContainerType;
        private readonly INamedTypeSymbol serviceCreatorCallbackType;
        private readonly Dictionary<IFieldSymbol, FieldAliasState> fieldAliases = new(SymbolEqualityComparer.Default);
        private readonly List<(IFieldSymbol Field, Location Location)> fieldInvocations = new();

        internal ServiceRegistrationAnalysisState(
            INamedTypeSymbol packageType,
            INamedTypeSymbol serviceContainerType,
            INamedTypeSymbol serviceCreatorCallbackType)
        {
            this.packageType = packageType;
            this.serviceContainerType = serviceContainerType;
            this.serviceCreatorCallbackType = serviceCreatorCallbackType;
        }

        internal void AnalyzeAssignment(OperationAnalysisContext context)
        {
            var assignment = (ISimpleAssignmentOperation)context.Operation;
            if (GetReferencedSymbol(assignment.Target) is not IFieldSymbol field ||
                !SymbolEqualityComparer.Default.Equals(field.ContainingType, this.packageType))
            {
                return;
            }

            bool isPackageAlias = IsContainingTypeInstance(
                assignment.Value,
                GetRootOperation(assignment),
                new HashSet<ISymbol>(SymbolEqualityComparer.Default));
            lock (this.syncObject)
            {
                if (!this.fieldAliases.TryGetValue(field, out FieldAliasState? aliasState))
                {
                    aliasState = new FieldAliasState();
                    this.fieldAliases.Add(field, aliasState);
                }

                aliasState.HasPackageAssignment |= isPackageAlias;
                aliasState.HasOtherAssignment |= !isPackageAlias;
            }
        }

        internal void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            if (invocation.TargetMethod.Name != Types.IServiceContainer.AddService ||
                !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, this.serviceContainerType) ||
                invocation.TargetMethod.Parameters.Length < 2 ||
                !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.Parameters[1].Type, this.serviceCreatorCallbackType))
            {
                return;
            }

            IArgumentOperation? serviceFactoryArgument = invocation.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == 1);
            Location location = serviceFactoryArgument?.Syntax.GetLocation() ?? invocation.Syntax.GetLocation();
            IOperation? instance = invocation.Instance;
            while (instance is IConversionOperation conversion)
            {
                instance = conversion.Operand;
            }

            if (instance is IFieldReferenceOperation fieldReference &&
                SymbolEqualityComparer.Default.Equals(fieldReference.Field.ContainingType, this.packageType))
            {
                lock (this.syncObject)
                {
                    this.fieldInvocations.Add((fieldReference.Field, location));
                }

                return;
            }

            if (IsContainingTypeInstance(instance, GetRootOperation(invocation), new HashSet<ISymbol>(SymbolEqualityComparer.Default)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, location));
            }
        }

        internal void AnalyzeSymbolEnd(SymbolAnalysisContext context)
        {
            lock (this.syncObject)
            {
                foreach ((IFieldSymbol field, Location location) in this.fieldInvocations)
                {
                    if (this.fieldAliases.TryGetValue(field, out FieldAliasState? aliasState) &&
                        aliasState.HasPackageAssignment &&
                        !aliasState.HasOtherAssignment)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, location));
                    }
                }
            }
        }

        private sealed class FieldAliasState
        {
            internal bool HasPackageAssignment { get; set; }

            internal bool HasOtherAssignment { get; set; }
        }
    }
}
