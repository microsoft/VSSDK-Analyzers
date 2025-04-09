// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        private static readonly Regex MemberReferenceRegex = new Regex(@"^\[(?<typeName>[^\[\]\:]+)+\]::(?<memberName>\S+)\s*$", RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);
        private static readonly char[] QualifiedIdentifierSeparators = new[] { '.' };
        internal ImmutableArray<TypeMatchSpec> MembersRequiringMainThread { get; set; }

        public static IEnumerable<TypeMatchSpec> ReadTypesAndMembers(AnalyzerOptions analyzerOptions, Regex fileNamePattern, CancellationToken cancellationToken)
        {
            var sampleTypes = """
                [Microsoft.VisualStudio.Shell.Interop]::SampleMethod
                [Microsoft.VisualStudio.Shell.ThreadHelper]::ThrowIfNotOnUIThread
                """;
            foreach (string line in sampleTypes.Split('\n'))
            {
                Match? match = null;
                try
                {
                    match = fileNamePattern.Match(line);
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
            context.RegisterCompilationStartAction(compilationContext =>
            {
                this.MembersRequiringMainThread = ReadTypesAndMembers(compilationContext.Options, MemberReferenceRegex, compilationContext.CancellationToken)
                .ToImmutableArray();

                // Check field initializers
                compilationContext.RegisterSyntaxNodeAction(
                    Utils.DebuggableWrapper(ctxt => this.AnalyzeClassDeclaration(ctxt)),
                    SyntaxKind.ClassDeclaration);

                // Check member access
                compilationContext.RegisterSyntaxNodeAction(
                    Utils.DebuggableWrapper(ctxt => this.AnalyzeMemberAccess(ctxt)),
                    SyntaxKind.SimpleMemberAccessExpression);

                // Check constructors
                compilationContext.RegisterSyntaxNodeAction(
                    Utils.DebuggableWrapper(ctxt => this.AnalyzeConstructorDeclaration(ctxt)),
                    SyntaxKind.ConstructorDeclaration);

                // Check IPartImportSatisfiedNotification.OnImportsSatisfied
                compilationContext.RegisterSyntaxNodeAction(
                    Utils.DebuggableWrapper(ctxt => this.AnalyzeMethodDeclaration(ctxt)),
                    SyntaxKind.MethodDeclaration);
            });
        }

        internal void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccessSyntax = (MemberAccessExpressionSyntax)context.Node;
            var property = context.SemanticModel.GetSymbolInfo(context.Node).Symbol as IPropertySymbol;
            if (property is object)
            {
                this.AnalyzeMemberWithinContext(property.ContainingType, property, context, memberAccessSyntax.Name.GetLocation());
            }
            else
            {
                var @event = context.SemanticModel.GetSymbolInfo(context.Node).Symbol as IEventSymbol;
                if (@event is object)
                {
                    this.AnalyzeMemberWithinContext(@event.ContainingType, @event, context, memberAccessSyntax.Name.GetLocation());
                }
            }
        }

        private void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context)
        {
            INamedTypeSymbol? importingConstructorAttribute = context.Compilation.GetTypeByMetadataName(Types.ImportingConstructorAttribute.FullName)?.OriginalDefinition;
            var declaration = (ConstructorDeclarationSyntax)context.Node;
            if (declaration.Body is not null
                && (declaration.ParameterList.Parameters.Count > 0
                || declaration.AttributeLists.Any(attrList => attrList.Attributes.Any(attr => context.SemanticModel.GetSymbolInfo(attr).Symbol?.ContainingType.Equals(importingConstructorAttribute) == true))))
            {
                this.AnalyzeStatements(context, declaration.Body.Statements);
            }
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            INamedTypeSymbol? partImportsSatisfiedNotificationInterface = context.Compilation.GetTypeByMetadataName(Types.IPartImportsSatisfiedNotification.FullName)?.OriginalDefinition;
            var declaration = (MethodDeclarationSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(declaration) as IMethodSymbol;

            if (partImportsSatisfiedNotificationInterface is not null
                && declaration.Body is not null
                && methodSymbol != null && methodSymbol.ContainingType.AllInterfaces.Contains(partImportsSatisfiedNotificationInterface))
            {
                this.AnalyzeStatements(context, declaration.Body.Statements);
            }
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            INamedTypeSymbol? exportAttributeType = context.Compilation.GetTypeByMetadataName(Types.ExportAttribute.FullName)?.OriginalDefinition;
            if (exportAttributeType is null)
            {
                return;
            }

            var declaration = (ClassDeclarationSyntax)context.Node;

            // Check if the class has the Export attribute
            bool hasExportAttribute = declaration.AttributeLists
                .SelectMany(attrList => attrList.Attributes)
                .Any(attr => context.SemanticModel.GetSymbolInfo(attr).Symbol?.ContainingType.Equals(exportAttributeType) == true);

            if (!hasExportAttribute)
            {
                return;
            }

            IEnumerable<SyntaxNode> toCheck = declaration.Members.OfType<FieldDeclarationSyntax>().Cast<SyntaxNode>()
                .Union(declaration.Members.OfType<PropertyDeclarationSyntax>().Cast<SyntaxNode>());
            this.AnalyzeStatements(context, toCheck);

            // TODO: Check eligible constructors - call AnalyzeConstructorDeclaration or AnalyzeMethodDeclaration from here
            foreach (var constructor in declaration.Members.OfType<ConstructorDeclarationSyntax>())
            {
                // AnalyzeConstructorDeclaration
            }
        }

        private void AnalyzeStatements(SyntaxNodeAnalysisContext context, IEnumerable<SyntaxNode> nodes)
        {
            var semanticModel = context.SemanticModel;
            foreach (var node in nodes)
            {
                // Check member accesses
                var memberAccesses = node.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
                foreach (var memberAccess in memberAccesses)
                {
                    var symbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
                    if (symbol is IPropertySymbol property)
                    {
                        this.AnalyzeMemberWithinContext(property.ContainingType, property, context, memberAccess.Name.GetLocation());
                    }
                    else if (symbol is IEventSymbol @event)
                    {
                        this.AnalyzeMemberWithinContext(@event.ContainingType, @event, context, memberAccess.Name.GetLocation());
                    }
                }

                // Check function calls
                var invocationExpressions = node.DescendantNodes().OfType<InvocationExpressionSyntax>();
                foreach (var invocation in invocationExpressions)
                {
                    var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (symbol is object)
                    {
                        this.AnalyzeMemberWithinContext(symbol.ContainingType, symbol, context, invocation.GetLocation());
                    }
                }

                // Check object creation expressions
                var objectCreations = node.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
                foreach (var objectCreation in objectCreations)
                {
                    var symbol = semanticModel.GetSymbolInfo(objectCreation).Symbol as IMethodSymbol;
                    if (symbol is object)
                    {
                        this.AnalyzeMemberWithinContext(symbol.ContainingType, symbol, context, objectCreation.GetLocation());
                    }
                }

                // Check assignment expressions
                var assignmentExpressions = node.DescendantNodes().OfType<AssignmentExpressionSyntax>();
                foreach (var assignment in assignmentExpressions)
                {
                    var symbol = semanticModel.GetSymbolInfo(assignment.Right).Symbol;
                    if (symbol is IPropertySymbol property)
                    {
                        this.AnalyzeMemberWithinContext(property.ContainingType, property, context, assignment.GetLocation());
                    }
                    else if (symbol is IEventSymbol @event)
                    {
                        this.AnalyzeMemberWithinContext(@event.ContainingType, @event, context, assignment.GetLocation());
                    }
                    else if (symbol is IMethodSymbol method)
                    {
                        this.AnalyzeMemberWithinContext(method.ContainingType, method, context, assignment.GetLocation());
                    }
                }
            }
        }

        private bool AnalyzeMemberWithinContext(ITypeSymbol type, ISymbol? symbol, SyntaxNodeAnalysisContext context, Location? focusDiagnosticOn = null)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            bool requiresUIThread = (type.TypeKind == TypeKind.Interface || type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
                && this.MembersRequiringMainThread.Contains(type, symbol);

            if (requiresUIThread)
            {
                Location location = focusDiagnosticOn ?? context.Node.GetLocation();
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

        private enum ThreadingContext
        {
            /// <summary>
            /// The context is not known, either because it was never asserted or switched to,
            /// or because a branch in the method exists which changed the context conditionally.
            /// </summary>
            Unknown,

            /// <summary>
            /// The context is definitely on the main thread.
            /// </summary>
            MainThread,

            /// <summary>
            /// The context is definitely on a non-UI thread.
            /// </summary>
            NotMainThread,
        }
    }
}
