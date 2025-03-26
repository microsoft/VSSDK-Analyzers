// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
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
                INamedTypeSymbol? exportAttributeType = compilationContext.Compilation.GetTypeByMetadataName(Types.ExportAttribute.FullName)?.OriginalDefinition;
                if (exportAttributeType is null)
                {
                    return;
                }

                INamedTypeSymbol? partImportsSatisfiedNotificationInterface = compilationContext.Compilation.GetTypeByMetadataName(Types.IPartImportsSatisfiedNotification.FullName)?.OriginalDefinition;
                INamedTypeSymbol? importingConstructorAttribute = compilationContext.Compilation.GetTypeByMetadataName(Types.ImportingConstructorAttribute.FullName)?.OriginalDefinition;

                // Check field initializers
                compilationContext.RegisterSyntaxNodeAction(
                    Utils.DebuggableWrapper(ctxt => this.AnalyzeClassDeclaration(ctxt)),
                    SyntaxKind.ClassDeclaration);

                // Check constructor
                compilationContext.RegisterSyntaxNodeAction(
                    Utils.DebuggableWrapper(ctxt => this.AnalyzeConstructorDeclaration(ctxt)),
                    SyntaxKind.ConstructorDeclaration);

                // Check ImportingConstructor and IPartImportSatisfiedNotification.OnImportsSatisfied
                compilationContext.RegisterSyntaxNodeAction(
                    Utils.DebuggableWrapper(ctxt => this.AnalyzeMethodDeclaration(ctxt, partImportsSatisfiedNotificationInterface, importingConstructorAttribute)),
                    SyntaxKind.MethodDeclaration);
            });
        }

        private void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declaration = (ConstructorDeclarationSyntax)context.Node;
            var attributes = declaration.AttributeLists;

        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol partImportsSatisfiedNotificationInterface, INamedTypeSymbol importingConstructorAttribute)
        {
            // Find constructor with [ImportingConstructor] and analyze it
            // Find method implementing IPartImportSatisfiedNotification.OnImportsSatisfied and analyze it
            var declaration = (MethodDeclarationSyntax)context.Node;
            var attributes = declaration.AttributeLists;
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            // Find constructor with no parameters and analyze it
            var declaration = (ClassDeclarationSyntax)context.Node;
            constructorWithNoParameter = declaration.GetMembers(ConstructorInfo.ConstructorName).OfType<IMethodSymbol>().Any(c => c.Parameters.Length == 1);
            BaseTypeSyntax? baseType = declaration.BaseList?.Types.FirstOrDefault(); // XXX: check interfaces
            if (baseType == null)
            {
                return;
            }
        }
    }
}
