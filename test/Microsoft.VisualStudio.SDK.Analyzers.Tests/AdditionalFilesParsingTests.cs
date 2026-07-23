// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SDK.Analyzers;
using Xunit;

public class AdditionalFilesParsingTests
{
    [Fact]
    public void TryParseNegatableTypeOrMemberReference_ParsesValidLine()
    {
        bool success = AdditionalFilesParsing.TryParseNegatableTypeOrMemberReference("![My.Namespace.Widget]::Initialize", out bool negated, out ReadOnlyMemory<char> typeName, out string? memberName);

        Assert.True(success);
        Assert.True(negated);
        Assert.Equal("My.Namespace.Widget", typeName.ToString());
        Assert.Equal("Initialize", memberName);
    }

    [Fact]
    public void TryParseNegatableTypeOrMemberReference_RejectsMalformedLine()
    {
        string malformed = "![[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[";

        bool success = AdditionalFilesParsing.TryParseNegatableTypeOrMemberReference(malformed, out _, out _, out _);

        Assert.False(success);
    }

    [Fact]
    public void TryParseMemberReference_ParsesValidLine()
    {
        bool success = AdditionalFilesParsing.TryParseMemberReference("[My.Namespace.Widget]::Initialize", out ReadOnlyMemory<char> typeName, out string? memberName);

        Assert.True(success);
        Assert.Equal("My.Namespace.Widget", typeName.ToString());
        Assert.Equal("Initialize", memberName);
    }

    [Fact]
    public void TryParseNegatableTypeOrMemberReference_ParsesTypeOnlyWithTrailingWhitespace()
    {
        bool success = AdditionalFilesParsing.TryParseNegatableTypeOrMemberReference("[My.Namespace.Widget]   \t", out bool negated, out ReadOnlyMemory<char> typeName, out string? memberName);

        Assert.True(success);
        Assert.False(negated);
        Assert.Equal("My.Namespace.Widget", typeName.ToString());
        Assert.Null(memberName);
    }

    [Theory]
    [InlineData("[My.Namespace.Widget]::")]
    [InlineData("!![My.Namespace.Widget]::Initialize")]
    [InlineData("[My.Namespace.Widget] ::Initialize")]
    [InlineData("[My.Namespace.Widget]:Initialize")]
    [InlineData("[My.Namespace.Widget]:::Initialize")]
    public void TryParseNegatableTypeOrMemberReference_RejectsMalformedCases(string input)
    {
        bool success = AdditionalFilesParsing.TryParseNegatableTypeOrMemberReference(input, out _, out _, out _);

        Assert.False(success);
    }

    [Theory]
    [InlineData("[My.Namespace.Widget]")]
    [InlineData("[My.Namespace.Widget]::Initialize extra")]
    [InlineData("[My.Namespace.Widget]:::Initialize")]
    public void TryParseMemberReference_RejectsMalformedCases(string input)
    {
        bool success = AdditionalFilesParsing.TryParseMemberReference(input, out _, out _);

        Assert.False(success);
    }
}
