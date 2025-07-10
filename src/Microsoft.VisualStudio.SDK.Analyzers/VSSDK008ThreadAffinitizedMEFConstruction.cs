// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    /// <summary>
    /// Identifies cases where a class decorated with [Export] accesses any member bound to UI thread from:
    /// - Constructor with no params.
    /// - Constructor decorated with [ImportingConstructor].
    /// - Field or property initializer.
    /// - implementation of IPartImportsSatisfiedNotification.OnImportsSatisfied.
    /// Supported edge cases:
    /// - Base class decorated with [InheritedExport].
    /// - class decorated with attribute derived from ExportAttribute.
    /// - exported member, not a class.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VSSDK008ThreadAffinitizedMEFConstruction : DiagnosticAnalyzer
    {
        /// <summary>
        /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
        /// </summary>
        public const string Id = "VSSDK008";

        /// <summary>
        /// A reusable descriptor for diagnostics produced by this analyzer.
        /// </summary>
        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: Id,
            title: "Avoid UI thread in MEF Part construction",
            messageFormat: "MEF part construction must not have UI thread affinity",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Reliability",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Descriptor];

        private ImmutableArray<TypeMatchSpec> MembersRequiringMainThread { get; set; }

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            // Register for compilation first so that we only activate the analyzer for applicable compilations
            context.RegisterCompilationStartAction(startCompilation =>
            {
                this.MembersRequiringMainThread = AdditionalFilesHelpers.GetMembersRequiringMainThread(startCompilation.Options, startCompilation.CancellationToken);
                INamedTypeSymbol? exportAttributeType = startCompilation.Compilation.GetTypeByMetadataName(Types.ExportAttribute.FullName)?.OriginalDefinition;
                INamedTypeSymbol? mef2ExportAttributeType = startCompilation.Compilation.GetTypeByMetadataName(Types.Mef2ExportAttribute.FullName)?.OriginalDefinition;
                INamedTypeSymbol? importingConstructorAttribute = startCompilation.Compilation.GetTypeByMetadataName(Types.ImportingConstructorAttribute.FullName)?.OriginalDefinition;
                INamedTypeSymbol? mef2ImportingConstructorAttribute = startCompilation.Compilation.GetTypeByMetadataName(Types.Mef2ImportingConstructorAttribute.FullName)?.OriginalDefinition;
                INamedTypeSymbol? inheritedExportAttribute = startCompilation.Compilation.GetTypeByMetadataName(Types.InheritedExportAttribute.FullName)?.OriginalDefinition;
                INamedTypeSymbol? partImportsSatisfiedNotificationInterface = startCompilation.Compilation.GetTypeByMetadataName(Types.IPartImportsSatisfiedNotification.FullName)?.OriginalDefinition;
                INamedTypeSymbol? mef2OnPartImportsSatisfiedAttribute = startCompilation.Compilation.GetTypeByMetadataName(Types.Mef2OnImportsSatisfiedAttribute.FullName)?.OriginalDefinition;

                if (exportAttributeType is null && mef2ExportAttributeType is null)
                {
                    // This code does not use MEF
                    return;
                }

                startCompilation.RegisterOperationAction(
                    Utils.DebuggableWrapper(context => this.AnalyzeOperation(
                        context,
                        exportAttributeType,
                        mef2ExportAttributeType,
                        importingConstructorAttribute,
                        mef2ImportingConstructorAttribute,
                        inheritedExportAttribute,
                        partImportsSatisfiedNotificationInterface,
                        mef2OnPartImportsSatisfiedAttribute)),
                    OperationKind.MethodReference,
                    OperationKind.InstanceReference,
                    OperationKind.FieldReference,
                    OperationKind.ObjectOrCollectionInitializer,
                    OperationKind.Invocation, // Method calls
                    OperationKind.MemberInitializer, // For static member access
                    OperationKind.PropertyReference, // For property access
                    OperationKind.ObjectCreation); // For object creation scenarios
            });
        }

        private void AnalyzeOperation(
            OperationAnalysisContext context,
            INamedTypeSymbol? exportAttributeType,
            INamedTypeSymbol? mef2ExportAttributeType,
            INamedTypeSymbol? importingConstructorAttribute,
            INamedTypeSymbol? mef2ImportingConstructorAttribute,
            INamedTypeSymbol? inheritedExportAttribute,
            INamedTypeSymbol? partImportsSatisfiedNotificationInterface,
            INamedTypeSymbol? mef2OnPartImportsSatisfiedAttribute)
        {
            ISymbol containingSymbol = context.ContainingSymbol;
            IOperation operation = context.Operation;
            INamedTypeSymbol containingType = containingSymbol.ContainingType;

            if (containingType is null)
            {
                // This code does not belong to a type. This analyzer does not apply.
                return;
            }

            if ((exportAttributeType is not null && containingSymbol.GetAttributes().Any(attr => Utils.IsEqualToOrDerivedFrom(attr.AttributeClass, exportAttributeType)))
                || (mef2ExportAttributeType is not null && containingSymbol.GetAttributes().Any(attr => Utils.IsEqualToOrDerivedFrom(attr.AttributeClass, mef2ExportAttributeType))))
            {
                // The member itself has an export attribute. Analyzer applies, there's no need to check the containing type.
            }
            else if ((exportAttributeType is not null && !containingType.GetAttributes().Any(attr => Utils.IsEqualToOrDerivedFrom(attr.AttributeClass, exportAttributeType)))
                && (mef2ExportAttributeType is not null && !containingType.GetAttributes().Any(attr => Utils.IsEqualToOrDerivedFrom(attr.AttributeClass, mef2ExportAttributeType))))
            {
                // It looks like this type is not a MEF part, so try to return early without checking it.

                // But first, check if any base type is decorated with [InheritedExport] attribute.
                INamedTypeSymbol? baseType = containingType.BaseType;
                if (inheritedExportAttribute is not null && baseType is not null)
                {
                    if (!Utils.HasInheritedAttribute(baseType, inheritedExportAttribute))
                    {
                        // Neither this type nor its base type is a MEF part.
                        return;
                    }
                }
                else
                {
                    // This type is not a MEF part, and it does not have a base class that might be a MEF part.
                    return;
                }
            }

            if (Utils.IsChildOfDelegateOrLambda(operation.Syntax))
            {
                // Don't test delegates or lambdas, because they are frequently invoked either asynchronously or lazily.
                // This is to reduce number of false positives (delegate initialized at construction).
                // We still suffer from a false positive of a "fire and forget" async method invoked in the constructor,
                // because it's very hard to detect such invocations that are awaited. In these edge cases, extender should suppress the warning.
                return;
            }

            // If there is a containing method, check if it's a parameterless constructor, or decorated with partImportsSatisfiedNotificationInterface or importingConstructorAttribute
            if (containingSymbol is IMethodSymbol methodSymbol)
            {
                if (methodSymbol is { MethodKind: MethodKind.Constructor, Parameters: [] })
                {
                    // Check if there is any other constructor decorated with importingConstructorAttribute
                    if (containingType.Constructors.Any(ctor =>
                        !SymbolEqualityComparer.Default.Equals(ctor, methodSymbol)
                        && ctor.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, importingConstructorAttribute))))
                    {
                        // A different constructor is marked as importing contructor. It's OK if this constructor has UI thread dependency.
                        return;
                    }

                    // Parameterless constructor must be free threaded
                }
                else if ((importingConstructorAttribute is not null && methodSymbol.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, importingConstructorAttribute)))
                    || (mef2ImportingConstructorAttribute is not null && methodSymbol.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, mef2ImportingConstructorAttribute))))
                {
                    // Constructor decorated with ImportingConstructorAttribute must be free threaded
                }
                else if (partImportsSatisfiedNotificationInterface is not null
                    && (methodSymbol.Name == Types.IPartImportsSatisfiedNotification.OnImportsSatisfied || methodSymbol.Name == Types.IPartImportsSatisfiedNotification.OnImportsSatisfiedFullName)
                    && containingType.AllInterfaces.Contains(partImportsSatisfiedNotificationInterface, SymbolEqualityComparer.Default))
                {
                    // OnImportsSatisfied implementation must be free threaded
                }
                else if (mef2OnPartImportsSatisfiedAttribute is not null && methodSymbol.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, mef2OnPartImportsSatisfiedAttribute)))
                {
                    // Method decorated with OnImportsSatisfiedAttribute must be free threaded
                }
                else
                {
                    // This analyzer does not enforce threading constraints on any other methods.
                    return;
                }
            }

            ISymbol? targetSymbol = GetSymbolFromOperation(operation);

            ISymbol? GetSymbolFromOperation(IOperation operation)
            {
                return operation switch
                {
                    IInvocationOperation op => op.TargetMethod,
                    IFieldReferenceOperation op => op.Field,
                    IPropertyReferenceOperation op => op.Property,
                    IMethodReferenceOperation op => op.Method,
                    IObjectCreationOperation op => op.Constructor,
                    IObjectOrCollectionInitializerOperation { Parent: { } parent } => GetSymbolFromOperation(parent),
                    IInstanceReferenceOperation { Parent: { } parent } => GetSymbolFromOperation(parent),
                    IMemberInitializerOperation { Parent: { } parent } => GetSymbolFromOperation(parent),
                    _ => null,
                };
            }

            if (targetSymbol != null)
            {
                // Analyze the target symbol within the context
                Location operationLocation = operation.Syntax.GetLocation();
                this.AnalyzeMemberWithinContext(targetSymbol.ContainingType, targetSymbol, context, operationLocation);
            }
        }

        private bool AnalyzeMemberWithinContext(ITypeSymbol type, ISymbol? symbol, OperationAnalysisContext context, Location location)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (this.MembersRequiringMainThread.Contains(type, symbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, location));
                return true;
            }

            return false;
        }
    }
}
