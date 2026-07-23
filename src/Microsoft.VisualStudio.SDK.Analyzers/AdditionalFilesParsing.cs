// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.SDK.Analyzers;

/// <summary>
/// Parsing helpers for additional-file lines used by <see cref="AdditionalFilesHelpers" />.
/// </summary>
internal static class AdditionalFilesParsing
{
    /// <summary>
    /// Parses a line that may begin with an optional <c>!</c>, followed by <c>[TypeName]</c>,
    /// and optionally <c>::MemberName</c>, without using regular expressions.
    /// </summary>
    /// <param name="line">The line to parse.</param>
    /// <param name="negated"><see langword="true" /> if the line begins with <c>!</c>.</param>
    /// <param name="typeName">The type name parsed from the brackets.</param>
    /// <param name="memberName">The member name after <c>::</c>, or <see langword="null" /> if not present.</param>
    /// <returns><see langword="true" /> if parsing succeeded.</returns>
    internal static bool TryParseNegatableTypeOrMemberReference(string line, out bool negated, out ReadOnlyMemory<char> typeName, out string? memberName)
    {
        negated = false;
        typeName = default;
        memberName = null;

        ReadOnlySpan<char> span = line.AsSpan();
        int pos = 0;

        if (pos < span.Length && span[pos] == '!')
        {
            negated = true;
            pos++;
        }

        int bracketStart = pos;
        if (!TryParseBracketedTypeName(span, ref pos, out _))
        {
            return false;
        }

        ReadOnlyMemory<char> typeNameMemory = line.AsMemory(bracketStart + 1, pos - bracketStart - 2);

        ReadOnlySpan<char> memberNameSpan = default;
        if (pos + 1 < span.Length && span[pos] == ':' && span[pos + 1] == ':')
        {
            pos += 2;
            int memberNameStart = pos;
            while (pos < span.Length && !char.IsWhiteSpace(span[pos]))
            {
                pos++;
            }

            if (pos == memberNameStart)
            {
                return false;
            }

            memberNameSpan = span.Slice(memberNameStart, pos - memberNameStart);
        }

        while (pos < span.Length && char.IsWhiteSpace(span[pos]))
        {
            pos++;
        }

        if (pos != span.Length)
        {
            return false;
        }

        typeName = typeNameMemory;
        memberName = memberNameSpan.IsEmpty ? null : memberNameSpan.ToString();
        return true;
    }

    /// <summary>
    /// Parses a line of the form <c>[TypeName]::MemberName</c>, without using regular expressions.
    /// </summary>
    /// <param name="line">The line to parse.</param>
    /// <param name="typeName">The type name parsed from the brackets.</param>
    /// <param name="memberName">The member name after <c>::</c>.</param>
    /// <returns><see langword="true" /> if parsing succeeded.</returns>
    internal static bool TryParseMemberReference(string line, out ReadOnlyMemory<char> typeName, [NotNullWhen(true)] out string? memberName)
    {
        typeName = default;
        memberName = null;

        ReadOnlySpan<char> span = line.AsSpan();
        int pos = 0;

        int bracketStart = pos;
        if (!TryParseBracketedTypeName(span, ref pos, out _))
        {
            return false;
        }

        ReadOnlyMemory<char> typeNameMemory = line.AsMemory(bracketStart + 1, pos - bracketStart - 2);

        if (pos + 1 >= span.Length || span[pos] != ':' || span[pos + 1] != ':')
        {
            return false;
        }

        pos += 2;

        int memberNameStart = pos;
        while (pos < span.Length && !char.IsWhiteSpace(span[pos]))
        {
            pos++;
        }

        if (pos == memberNameStart)
        {
            return false;
        }

        ReadOnlySpan<char> memberNameSpan = span.Slice(memberNameStart, pos - memberNameStart);

        while (pos < span.Length && char.IsWhiteSpace(span[pos]))
        {
            pos++;
        }

        if (pos != span.Length)
        {
            return false;
        }

        typeName = typeNameMemory;
        memberName = memberNameSpan.ToString();
        return true;
    }

    /// <summary>
    /// Advances <paramref name="pos" /> past a <c>[TypeName]</c> token and outputs the type-name span.
    /// </summary>
    /// <param name="span">The full input span.</param>
    /// <param name="pos">The current parse position; advanced past the closing <c>]</c> on success.</param>
    /// <param name="typeName">A slice of <paramref name="span" /> containing the type name, without the brackets.</param>
    /// <returns><see langword="true" /> if a non-empty bracketed type name was consumed.</returns>
    internal static bool TryParseBracketedTypeName(ReadOnlySpan<char> span, ref int pos, out ReadOnlySpan<char> typeName)
    {
        typeName = default;

        if (pos >= span.Length || span[pos] != '[')
        {
            return false;
        }

        pos++;

        int typeNameStart = pos;
        while (pos < span.Length && span[pos] != '[' && span[pos] != ']' && span[pos] != ':')
        {
            pos++;
        }

        if (pos == typeNameStart)
        {
            return false;
        }

        typeName = span.Slice(typeNameStart, pos - typeNameStart);

        if (pos >= span.Length || span[pos] != ']')
        {
            return false;
        }

        pos++;
        return true;
    }
}
