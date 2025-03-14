﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Domain.Apps.Core.TestHelpers;
using Squidex.Infrastructure.Collections;
using Squidex.Infrastructure.Json.Objects;

namespace Squidex.Domain.Apps.Core.Operations.ValidateContent;

public class StringFieldTests : IClassFixture<TranslationsFixture>
{
    private readonly List<string> errors = [];

    [Fact]
    public void Should_instantiate_field()
    {
        var sut = Field(new StringFieldProperties());

        Assert.Equal("myString", sut.Name);
    }

    [Fact]
    public async Task Should_not_add_error_if_string_is_valid()
    {
        var sut = Field(new StringFieldProperties { Label = "<FIELD>" });

        await sut.ValidateAsync(CreateValue(null), errors);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Should_add_error_if_string_is_required_but_null()
    {
        var sut = Field(new StringFieldProperties { IsRequired = true });

        await sut.ValidateAsync(CreateValue(null), errors);

        errors.Should().BeEquivalentTo(
            ["Field is required."]);
    }

    [Fact]
    public async Task Should_add_error_if_string_is_required_but_empty()
    {
        var sut = Field(new StringFieldProperties { IsRequired = true });

        await sut.ValidateAsync(CreateValue(string.Empty), errors);

        errors.Should().BeEquivalentTo(
            ["Field is required."]);
    }

    [Fact]
    public async Task Should_add_error_if_string_is_shorter_than_min_length()
    {
        var sut = Field(new StringFieldProperties { MinLength = 10 });

        await sut.ValidateAsync(CreateValue("123"), errors);

        errors.Should().BeEquivalentTo(
            ["Must have at least 10 character(s)."]);
    }

    [Fact]
    public async Task Should_add_error_if_string_is_longer_than_max_length()
    {
        var sut = Field(new StringFieldProperties { MaxLength = 5 });

        await sut.ValidateAsync(CreateValue("12345678"), errors);

        errors.Should().BeEquivalentTo(
            ["Must not have more than 5 character(s)."]);
    }

    [Fact]
    public async Task Should_add_error_if_string_is_shorter_than_min_characters()
    {
        var sut = Field(new StringFieldProperties { MinCharacters = 10 });

        await sut.ValidateAsync(CreateValue("123"), errors);

        errors.Should().BeEquivalentTo(
            ["Must have at least 10 text character(s)."]);
    }

    [Fact]
    public async Task Should_add_error_if_string_is_longer_than_max_characters()
    {
        var sut = Field(new StringFieldProperties { MaxCharacters = 5 });

        await sut.ValidateAsync(CreateValue("123456"), errors);

        errors.Should().BeEquivalentTo(
            ["Must not have more than 5 text character(s)."]);
    }

    [Fact]
    public async Task Should_add_error_if_string_is_shorter_than_min_words()
    {
        var sut = Field(new StringFieldProperties { MinWords = 10 });

        await sut.ValidateAsync(CreateValue("word1 word2 word3"), errors);

        errors.Should().BeEquivalentTo(
            ["Must have at least 10 word(s)."]);
    }

    [Fact]
    public async Task Should_add_error_if_string_is_longer_than_max_words()
    {
        var sut = Field(new StringFieldProperties { MaxWords = 5 });

        await sut.ValidateAsync(CreateValue("word1 word2 word3 word4 word5 word6"), errors);

        errors.Should().BeEquivalentTo(
            ["Must not have more than 5 word(s)."]);
    }

    [Fact]
    public async Task Should_add_error_if_string_not_allowed()
    {
        var sut = Field(new StringFieldProperties { AllowedValues = ReadonlyList.Create("Foo") });

        await sut.ValidateAsync(CreateValue("Bar"), errors);

        errors.Should().BeEquivalentTo(
            ["Not an allowed value."]);
    }

    [Fact]
    public async Task Should_add_error_if_number_is_not_valid_pattern()
    {
        var sut = Field(new StringFieldProperties { Pattern = "[0-9]{3}" });

        await sut.ValidateAsync(CreateValue("abc"), errors);

        errors.Should().BeEquivalentTo(
            ["Must follow the pattern."]);
    }

    [Fact]
    public async Task Should_add_error_if_number_is_not_valid_pattern_with_message()
    {
        var sut = Field(new StringFieldProperties { Pattern = "[0-9]{3}", PatternMessage = "Custom Error Message." });

        await sut.ValidateAsync(CreateValue("abc"), errors);

        errors.Should().BeEquivalentTo(
            ["Custom Error Message."]);
    }

    private static JsonValue CreateValue(string? v)
    {
        return JsonValue.Create(v);
    }

    private static RootField<StringFieldProperties> Field(StringFieldProperties properties)
    {
        return Fields.String(1, "myString", Partitioning.Invariant, properties);
    }
}
