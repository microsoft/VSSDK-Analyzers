// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    /// <summary>
    /// Identifies cases where a class decorated with [Export] accesses any member bound to UI thread from
    /// - Constructor with no params
    /// - Constructor decorated with [ImportingConstructor]
    /// - Field or property initializer
    /// - implementation of IPartImportsSatisfiedNotification.OnImportsSatisfied.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VSSDK013ThreadAffinitizedMEFConstruction : DiagnosticAnalyzer
    {
        /// <summary>
        /// The value to use for <see cref="DiagnosticDescriptor.Id"/> in generated diagnostics.
        /// </summary>
        public const string Id = "VSSDK013";

        /// <summary>
        /// A reusable descriptor for diagnostics produced by this analyzer.
        /// </summary>
        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: Id,
            title: "Free threaded MEF Part construction",
            messageFormat: "MEF Parts construction should not have UI thread affinity",
            helpLinkUri: Utils.GetHelpLink(Id),
            category: "Reliability",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

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
                INamedTypeSymbol? importingConstructorAttribute = startCompilation.Compilation.GetTypeByMetadataName(Types.ImportingConstructorAttribute.FullName)?.OriginalDefinition;
                INamedTypeSymbol? partImportsSatisfiedNotificationInterface = startCompilation.Compilation.GetTypeByMetadataName(Types.IPartImportsSatisfiedNotification.FullName)?.OriginalDefinition;
                INamedTypeSymbol? exportAttributeType = startCompilation.Compilation.GetTypeByMetadataName(Types.ExportAttribute.FullName)?.OriginalDefinition;

                var operationKinds = ImmutableArray.Create<OperationKind>(
                    OperationKind.MethodReference,
                    OperationKind.InstanceReference,
                    OperationKind.FieldReference,
                    OperationKind.ObjectOrCollectionInitializer,
                    OperationKind.Invocation, // Method calls
                    OperationKind.MemberInitializer, // For static member access
                    OperationKind.PropertyReference, // For property access
                    OperationKind.ObjectCreation); // For object creation scenarios

                startCompilation.RegisterOperationAction(
                    Utils.DebuggableWrapper(c => this.AnalyzeOperation(c, exportAttributeType, importingConstructorAttribute, partImportsSatisfiedNotificationInterface)),
                    operationKinds);
            });
        }



        private void AnalyzeOperation(
            OperationAnalysisContext c,
            INamedTypeSymbol? exportAttributeType,
            INamedTypeSymbol? importingConstructorAttribute,
            INamedTypeSymbol? partImportsSatisfiedNotificationInterface)
        {
            ISymbol containingSymbol = c.ContainingSymbol;
            IOperation operation = c.Operation;
            INamedTypeSymbol containingType = containingSymbol.ContainingType;

            if (containingType is null || exportAttributeType is null)
            {
                // This code does not belong to a type, or we can't get a hold of [Export]Attribute. This analyzer does not apply.
                return;
            }

            if (!containingType.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, exportAttributeType)))
            {
                // This type is not a MEF part, don't check it.
                return;
            }

            // If there is a containing method, check if it's a parameterless constructor, or decorated with partImportsSatisfiedNotificationInterface or importingConstructorAttribute
            if (containingSymbol is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.MethodKind == MethodKind.Constructor && methodSymbol.Parameters.Length == 0)
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
                else if (methodSymbol.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, importingConstructorAttribute)))
                {
                    // Constructor decorated with ImportingConstructorAttribute must be free threaded
                }
                else if (partImportsSatisfiedNotificationInterface is not null
                    && (methodSymbol.Name == Types.IPartImportsSatisfiedNotification.OnImportsSatisfied || methodSymbol.Name == Types.IPartImportsSatisfiedNotification.OnImportsSatisfiedFullName)
                    && containingType.AllInterfaces.Contains(partImportsSatisfiedNotificationInterface, SymbolEqualityComparer.Default))
                {
                    // OnImportsSatisfied implementation must be free threaded
                }
                else
                {
                    // This analyzer does not enforce threading contstraints on any other methods.
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
                    IObjectOrCollectionInitializerOperation op => op.Parent is not null ? GetSymbolFromOperation(op.Parent) : null,
                    IInstanceReferenceOperation op => op.Parent is not null ? GetSymbolFromOperation(op.Parent) : null,
                    IMemberInitializerOperation op => op.Parent is not null ? GetSymbolFromOperation(op.Parent) : null,
                    _ => null,
                };
            }

            if (targetSymbol != null)
            {
                // Analyze the target symbol within the context
                Location operationLocation = operation.Syntax.GetLocation();
                Location? firstSyntaxlocation = targetSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(c.CancellationToken).GetLocation();
                this.AnalyzeMemberWithinContext(targetSymbol.ContainingType, targetSymbol, c, firstSyntaxlocation ?? operationLocation);
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
