using FluentAssertions;
using Ogasela.Application.Marketing.Announcements;

namespace Ogasela.UnitTests.Marketing;

public class CreateAnnouncementCommandValidatorTests
{
    private static CreateAnnouncementCommand Command(string? ctaLabel, string? ctaUrl) =>
        new("Black Friday", "Up to 50% off featured ads this week.", Stream.Null, "promo.jpg", "image/jpeg", ctaLabel, ctaUrl, true);

    [Theory]
    [InlineData(null, null)]
    [InlineData("Shop now", "https://ogasela.com/deals")]
    [InlineData("Post an ad", "ogasela://post")]
    public void Valid_WithNoButtonOrAProperLink(string? label, string? url) =>
        new CreateAnnouncementCommandValidator().Validate(Command(label, url)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("Shop now", null)]
    [InlineData(null, "https://ogasela.com")]
    [InlineData("Shop now", "javascript:alert(1)")]
    [InlineData("Shop now", "not a url")]
    public void Invalid_WithHalfAButtonOrAnUnsafeLink(string? label, string? url) =>
        new CreateAnnouncementCommandValidator().Validate(Command(label, url)).IsValid.Should().BeFalse();
}
