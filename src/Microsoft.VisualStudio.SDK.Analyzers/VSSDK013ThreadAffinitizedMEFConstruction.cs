// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    // TODO: Learn from VSTHRD010MainThreadUsageAnalyzer
    // https://microsoft.github.io/vs-threading/analyzers/VSTHRD010.html
    // particularly GetTransitiveClosureOfMainThreadRequiringMethods

    /// <summary>
    /// Identifies cases where a class decorated with [Export] accesses any member bound to UI thread from
    /// - Constructor with no params
    /// - Constructor decorated with [ImportingConstructor]
    /// - Field or property initializer
    /// - implementation of IPartImportsSatisfiedNotification.OnImportsSatisfied
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
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        /// <summary>
        /// A cached array for the <see cref="SupportedDiagnostics"/> property.
        /// </summary>
        private static readonly ImmutableArray<DiagnosticDescriptor> ReusableSupportedDiagnostics = ImmutableArray.Create(Descriptor);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ReusableSupportedDiagnostics;

        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5);  // Prevent expensive CPU hang in Regex.Match if backtracking occurs due to pathological input (see vs-threading #485).
        private static readonly Regex NegatableTypeOrMemberReferenceRegex = new Regex(@"^(?<negated>!)?\[(?<typeName>[^\[\]\:]+)+\](?:\:\:(?<memberName>\S+))?\s*$", RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);
        private static readonly char[] QualifiedIdentifierSeparators = new[] { '.' };
        public static readonly Regex FileNamePatternForMembersRequiringMainThread = new Regex(@"^vs-threading\.MembersRequiringMainThread(\..*)?.txt$", FileNamePatternRegexOptions);
        private const RegexOptions FileNamePatternRegexOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;

        internal ImmutableArray<TypeMatchSpec> MembersRequiringMainThread { get; set; }

        public static IEnumerable<TypeMatchSpec> ReadTypesAndMembers(AnalyzerOptions analyzerOptions, Regex fileNamePattern, CancellationToken cancellationToken)
        {
            // TODO: load from files matching fileNamePattern instead
            var sampleTypes = """
                [Microsoft.VisualStudio.Shell.UIContext]
                [Microsoft.VisualStudio.Shell.ThreadHelper]::ThrowIfNotOnUIThread
                """;
            foreach (string line in sampleTypes.Split('\n'))
            {
                Match? match = null;
                try
                {
                    match = NegatableTypeOrMemberReferenceRegex.Match(line);
                }
                catch (RegexMatchTimeoutException)
                {
                    throw new InvalidOperationException($"Regex.Match timeout when parsing line: {line}");
                }

                if (!match.Success)
                {
                    throw new InvalidOperationException($"Parsing error on line: {line}");
                }

                bool inverted = match.Groups["negated"].Success;
                string[] typeNameElements = match.Groups["typeName"].Value.Split(QualifiedIdentifierSeparators);
                string typeName = typeNameElements[typeNameElements.Length - 1];
                var containingNamespace = typeNameElements.Take(typeNameElements.Length - 1).ToImmutableArray();
                var type = new QualifiedType(containingNamespace, typeName);
                QualifiedMember member = match.Groups["memberName"].Success ? new QualifiedMember(type, match.Groups["memberName"].Value) : default(QualifiedMember);
                yield return new TypeMatchSpec(type, member, inverted);
            }
        }

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
                this.MembersRequiringMainThread = ReadTypesAndMembers(startCompilation.Options, FileNamePatternForMembersRequiringMainThread, startCompilation.CancellationToken).ToImmutableArray();
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
                    OperationKind.ObjectCreation // For object creation scenarios
                );
                startCompilation.RegisterOperationAction(
                    Utils.DebuggableWrapper(c => this.AnalyzeOperation(c, exportAttributeType, importingConstructorAttribute, partImportsSatisfiedNotificationInterface)),
                    operationKinds);
                /*
                if (importingConstructorAttribute is object)
                {
                    // Check decorated constructor
                    startCompilation.RegisterSymbolAction(Utils.DebuggableWrapper(c => this.AnalyzeMethod(c, importingConstructorAttribute)), SymbolKind.Method);
                }

                if (partImportsSatisfiedNotificationInterface is object)
                {
                    // Check interface implementation
                    startCompilation.RegisterSymbolAction(Utils.DebuggableWrapper(c => this.AnalyzeImportSatisfied(c, partImportsSatisfiedNotificationInterface)), SymbolKind.NamedType);
                }

                if (exportAttributeType is object)
                {
                    // Check constructor and field initializers
                    startCompilation.RegisterSymbolAction(Utils.DebuggableWrapper(c => this.AnalyzeType(c, exportAttributeType)), SymbolKind.NamedType);
                }
                */
            });
        }

        private void AnalyzeOperation(
            OperationAnalysisContext c,
            INamedTypeSymbol? exportAttributeType,
            INamedTypeSymbol? importingConstructorAttribute,
            INamedTypeSymbol? partImportsSatisfiedNotificationInterface)
        {
            var containingSymbol = c.ContainingSymbol;
            var operation = c.Operation;
            var containingType = containingSymbol.ContainingType;

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
                    // Parameterless constructor must be free threaded
                }
                else if (methodSymbol.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, importingConstructorAttribute)))
                {
                    // Constructor decorated with ImportingConstructorAttribute must be free threaded
                }
                else if (methodSymbol.Name == Types.IPartImportsSatisfiedNotification.OnImportsSatisfied
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

            ISymbol? targetSymbol = null;

            // Use pattern matching to handle different operation types
            switch (operation)
            {
                case IInvocationOperation invocationOperation:
                    targetSymbol = invocationOperation.TargetMethod;
                    break;

                case IFieldReferenceOperation fieldReferenceOperation:
                    targetSymbol = fieldReferenceOperation.Field;
                    break;

                case IPropertyReferenceOperation propertyReferenceOperation:
                    targetSymbol = propertyReferenceOperation.Property;
                    break;

                case IMethodReferenceOperation methodReferenceOperation:
                    targetSymbol = methodReferenceOperation.Method;
                    break;

                case IObjectCreationOperation objectCreationOperation:
                    targetSymbol = objectCreationOperation.Constructor;
                    break;

                case IInstanceReferenceOperation instanceReferenceOperation:
                    // not sure how to handle this
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (targetSymbol != null)
            {
                // Analyze the target symbol within the context
                var operationLocation = operation.Syntax.GetLocation();
                var firstSyntaxlocation = targetSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(c.CancellationToken).GetLocation();
                this.AnalyzeMemberWithinContext(targetSymbol.ContainingType, targetSymbol, c, firstSyntaxlocation ?? operationLocation);
            }
        }

        internal void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol importingConstructorAttribute)
        {
            // Ensure the symbol is a method
            if (context.Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            // Check if the method is a constructor
            if (methodSymbol.MethodKind != MethodKind.Constructor)
            {
                return;
            }

            // Check if the constructor is decorated with the importingConstructorAttribute
            foreach (var attribute in methodSymbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, importingConstructorAttribute))
                {
                    // Analyze all statements within the method
                    this.AnalyzeMethodContents(context, methodSymbol);
                }
            }
        }

        internal void AnalyzeImportSatisfied(SymbolAnalysisContext context, INamedTypeSymbol partImportsSatisfiedNotificationInterface)
        {
            // Ensure the symbol is a named type
            if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return;
            }

            // Check if the type implements the IPartImportsSatisfiedNotification interface
            if (!namedTypeSymbol.AllInterfaces.Contains(partImportsSatisfiedNotificationInterface, SymbolEqualityComparer.Default))
            {
                return;
            }

            // Look for the OnImportsSatisfied method in the type
            var candidateSymbols = namedTypeSymbol
                .GetMembers()
                .OfType<IMethodSymbol>();
            var explicitImplementations = candidateSymbols
                .Where(n => n.MethodKind == MethodKind.ExplicitInterfaceImplementation && n.Name == Types.IPartImportsSatisfiedNotification.OnImportsSatisfiedFullName);
            var implicitImplementations = candidateSymbols
                .Where(n => n.MethodKind == MethodKind.Ordinary && n.Parameters.Length == 0 && n.Name.EndsWith(Types.IPartImportsSatisfiedNotification.OnImportsSatisfied));

            if (explicitImplementations.Any() || implicitImplementations.Any())
            {
                this.AnalyzeMethodContents(context, explicitImplementations.FirstOrDefault() ?? implicitImplementations.First());
            }
        }

        private void AnalyzeMethodContents(SymbolAnalysisContext context, IMethodSymbol matchingMethodSymbol)
        {
            // Analyze all statements within the method
            var syntaxReference = matchingMethodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxReference != null)
            {
                var baseMethodDeclaration = syntaxReference.GetSyntax(context.CancellationToken) as BaseMethodDeclarationSyntax;
                if (baseMethodDeclaration != null)
                {
                    foreach (var statement in baseMethodDeclaration.Body?.Statements ?? Enumerable.Empty<StatementSyntax>())
                    {
                        var semanticModel = context.Compilation.GetSemanticModel(statement.SyntaxTree);
                        var expressionStatement = statement as ExpressionStatementSyntax;
                        if (expressionStatement?.Expression is InvocationExpressionSyntax invocationExpression)
                        {
                            var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression.Expression, context.CancellationToken).Symbol;
                            this.AnalyzeMemberWithinContext(symbolInfo.ContainingType, symbolInfo, context, statement.GetLocation());
                        }
                    }
                }
            }
        }

        internal void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol exportAttributeType)
        {
            // Ensure the symbol is a named type
            if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return;
            }

            // Check if the class is decorated with the Export attribute
            if (!namedTypeSymbol.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, exportAttributeType)))
            {
                return;
            }

            // Find constructors with no parameters
            var parameterlessConstructors = namedTypeSymbol.Constructors
                .Where(ctor => ctor.Parameters.Length == 0);

            foreach (var parameterlessConstructor in parameterlessConstructors)
            {
                this.AnalyzeMethodContents(context, parameterlessConstructor);
            }

            // Find fields and properties with initializers
            var fieldsWithInitializers = namedTypeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(field => field.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntax())
                    .OfType<VariableDeclaratorSyntax>()
                    .Any(declarator => declarator.Initializer != null));

            foreach (var field in fieldsWithInitializers)
            {
                var declaratorSyntax = field.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntax())
                    .OfType<VariableDeclaratorSyntax>()
                    .FirstOrDefault(declarator => declarator.Initializer != null);

                if (declaratorSyntax?.Initializer?.Value is ExpressionSyntax initializerExpression)
                {
                    var semanticModel = context.Compilation.GetSemanticModel(initializerExpression.SyntaxTree);

                    // Traverse the initializer expression to analyze all symbols
                    foreach (var descendantNode in initializerExpression.DescendantNodesAndSelf())
                    {
                        var symbol = semanticModel.GetSymbolInfo(descendantNode, context.CancellationToken).Symbol;
                        if (symbol?.ContainingType != null)
                        {
                            this.AnalyzeMemberWithinContext(symbol.ContainingType, symbol, context, descendantNode.GetLocation());
                        }
                    }
                }
            }

            var propertiesWithInitializers = namedTypeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(property => property.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntax())
                    .OfType<PropertyDeclarationSyntax>()
                    .Any(declaration => declaration.Initializer != null));

            foreach (var property in propertiesWithInitializers)
            {
                var propertyDeclarationSyntax = property.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntax())
                    .OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(declaration => declaration.Initializer != null);

                if (propertyDeclarationSyntax?.Initializer?.Value is ExpressionSyntax initializerExpression)
                {
                    var semanticModel = context.Compilation.GetSemanticModel(initializerExpression.SyntaxTree);

                    // Traverse the initializer expression to analyze all symbols
                    foreach (var descendantNode in initializerExpression.DescendantNodesAndSelf())
                    {
                        var symbol = semanticModel.GetSymbolInfo(descendantNode, context.CancellationToken).Symbol;
                        if (symbol?.ContainingType != null)
                        {
                            this.AnalyzeMemberWithinContext(symbol.ContainingType, symbol, context, descendantNode.GetLocation());
                        }
                    }
                }
            }
        }

        private bool AnalyzeMemberWithinContext(ITypeSymbol type, ISymbol? symbol, SymbolAnalysisContext context, Location location)
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

        internal static readonly IImmutableSet<SyntaxKind> MethodSyntaxKinds = ImmutableHashSet.Create(
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.AnonymousMethodExpression,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.GetAccessorDeclaration,
            SyntaxKind.SetAccessorDeclaration,
            SyntaxKind.AddAccessorDeclaration,
            SyntaxKind.RemoveAccessorDeclaration);
    }
}
