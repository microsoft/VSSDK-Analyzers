// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
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
        private const RegexOptions FileNamePatternRegexOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;

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

        public static readonly Regex FileNamePatternForMembersRequiringMainThread = new Regex(@"^vs-threading\.MembersRequiringMainThread(\..*)?.txt$", FileNamePatternRegexOptions);

        /// <summary>
        /// A cached array for the <see cref="SupportedDiagnostics"/> property.
        /// </summary>
        private static readonly ImmutableArray<DiagnosticDescriptor> ReusableSupportedDiagnostics = ImmutableArray.Create(Descriptor);
        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5);  // Prevent expensive CPU hang in Regex.Match if backtracking occurs due to pathological input (see vs-threading #485).
        private static readonly Regex NegatableTypeOrMemberReferenceRegex = new Regex(@"^(?<negated>!)?\[(?<typeName>[^\[\]\:]+)+\](?:\:\:(?<memberName>\S+))?\s*$", RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);
        private static readonly char[] QualifiedIdentifierSeparators = new[] { '.' };

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ReusableSupportedDiagnostics;

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
                else if ((methodSymbol.Name == Types.IPartImportsSatisfiedNotification.OnImportsSatisfied || methodSymbol.Name == Types.IPartImportsSatisfiedNotification.OnImportsSatisfiedFullName)
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
