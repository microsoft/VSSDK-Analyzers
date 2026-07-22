// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.SDK.Analyzers
{
    /// <summary>
    /// Helper class for accessing AdditionalFiles, which define list of APIs to check.
    /// We have access to AdditionalFiles provided by Microsoft.VisualStudio.Threading.Analyzers,
    /// as a transitive dependency of Microsoft.VisualStudio.Threading.
    /// </summary>
    internal class AdditionalFilesHelpers
    {
        private const RegexOptions FileNamePatternRegexOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        private static readonly Regex FileNamePatternForLegacyThreadSwitchingMembers = new Regex(@"^vs-threading\.LegacyThreadSwitchingMembers(\..*)?.txt$", FileNamePatternRegexOptions);
        private static readonly Regex FileNamePatternForMembersRequiringMainThread = new Regex(@"^vs-threading\.MembersRequiringMainThread(\..*)?.txt$", FileNamePatternRegexOptions);
        private static readonly Regex FileNamePatternForMethodsThatAssertMainThread = new Regex(@"^vs-threading\.MainThreadAssertingMethods(\..*)?.txt$", FileNamePatternRegexOptions);
        private static readonly Regex FileNamePatternForMethodsThatSwitchToMainThread = new Regex(@"^vs-threading\.MainThreadSwitchingMethods(\..*)?.txt$", FileNamePatternRegexOptions);

        /// <summary>
        /// Gets memebers that require main thread, from all available files matching <see cref="FileNamePatternForMembersRequiringMainThread"/>.
        /// </summary>
        /// <param name="analyzerOptions">Compilation options.</param>
        /// <param name="cancellationToken">Compilation cancellation token.</param>
        /// <returns>Array of members that require, assert, or switch to Main thread.</returns>
        public static ImmutableArray<TypeMatchSpec> GetMembersRequiringMainThread(AnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
        {
            return ReadTypesAndMembers(analyzerOptions, FileNamePatternForLegacyThreadSwitchingMembers, cancellationToken)
                .Union(ReadTypesAndMembers(analyzerOptions, FileNamePatternForMembersRequiringMainThread, cancellationToken))
                .Union(ReadTypesAndMembers(analyzerOptions, FileNamePatternForMethodsThatAssertMainThread, cancellationToken))
                .Union(ReadTypesAndMembers(analyzerOptions, FileNamePatternForMethodsThatSwitchToMainThread, cancellationToken))
                .ToImmutableArray();
        }

        private static IEnumerable<TypeMatchSpec> ReadTypesAndMembers(AnalyzerOptions analyzerOptions, Regex fileNamePattern, CancellationToken cancellationToken)
        {
            foreach (string line in ReadAdditionalFiles(analyzerOptions, fileNamePattern, cancellationToken))
            {
                if (!AdditionalFilesParsing.TryParseNegatableTypeOrMemberReference(line, out bool negated, out ReadOnlyMemory<char> typeNameMemory, out string? memberNameValue))
                {
                    throw new InvalidOperationException($"Parsing error on line: {line}");
                }

                (ImmutableArray<string> containingNamespace, string typeName) = SplitQualifiedIdentifier(typeNameMemory);
                var type = new QualifiedType(containingNamespace, typeName);
                QualifiedMember member = memberNameValue is not null ? new QualifiedMember(type, memberNameValue) : default(QualifiedMember);
                yield return new TypeMatchSpec(type, member, negated);
            }
        }

        private static (ImmutableArray<string> ContainingNamespace, string TypeName) SplitQualifiedIdentifier(ReadOnlyMemory<char> qualifiedName)
        {
            ReadOnlySpan<char> qualifiedNameSpan = qualifiedName.Span;
            int lastDot = qualifiedNameSpan.LastIndexOf('.');
            if (lastDot < 0)
            {
                return (ImmutableArray<string>.Empty, qualifiedName.ToString());
            }

            string typeName = qualifiedName.Slice(lastDot + 1).ToString();
            ReadOnlySpan<char> namespacePart = qualifiedNameSpan.Slice(0, lastDot);
            ImmutableArray<string>.Builder namespaceBuilder = ImmutableArray.CreateBuilder<string>();

            int segmentStart = 0;
            for (int i = 0; i <= namespacePart.Length; i++)
            {
                if (i == namespacePart.Length || namespacePart[i] == '.')
                {
                    namespaceBuilder.Add(namespacePart.Slice(segmentStart, i - segmentStart).ToString());
                    segmentStart = i + 1;
                }
            }

            return (namespaceBuilder.ToImmutable(), typeName);
        }

        private static IEnumerable<string> ReadAdditionalFiles(AnalyzerOptions analyzerOptions, Regex fileNamePattern, CancellationToken cancellationToken)
        {
            if (analyzerOptions is null)
            {
                throw new ArgumentNullException(nameof(analyzerOptions));
            }

            if (fileNamePattern is null)
            {
                throw new ArgumentNullException(nameof(fileNamePattern));
            }

            IEnumerable<SourceText>? docs = from file in analyzerOptions.AdditionalFiles.OrderBy(x => x.Path, StringComparer.Ordinal)
                                            let fileName = Path.GetFileName(file.Path)
                                            where fileNamePattern.IsMatch(fileName)
                                            let text = file.GetText(cancellationToken)
                                            select text;
            return docs.SelectMany(ReadLinesFromAdditionalFile);
        }

        private static IEnumerable<string> ReadLinesFromAdditionalFile(SourceText text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            foreach (TextLine line in text.Lines)
            {
                string lineText = line.ToString();

                if (!string.IsNullOrWhiteSpace(lineText) && !lineText.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                {
                    yield return lineText;
                }
            }
        }
    }
}
