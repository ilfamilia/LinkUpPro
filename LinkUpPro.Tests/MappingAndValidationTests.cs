using System.ComponentModel.DataAnnotations;
using AutoMapper;
using LinkUpPro.Core.Application.Mappings;
using LinkUpPro.Helpers;
using LinkUpPro.Mappings;
using LinkUpPro.ViewModels.Battleship;
using LinkUpPro.ViewModels.Social;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LinkUpPro.Tests;

/// <summary>Checks mapping contracts and meaningful input boundaries used by the actual MVC forms.</summary>
public sealed class MappingAndValidationTests
{
    [Fact]
    public void ApplicationAndWebMappingProfilesAreValid()
    {
        var configuration = new MapperConfiguration(c =>
        {
            c.AddProfile<SocialMappingProfile>();
            c.AddProfile<SocialWebMappingProfile>();
        }, NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(12, 0)]
    [InlineData(0, 12)]
    public void AttackViewModelRejectsOffBoardCoordinates(int row, int column)
        => Assert.False(Validate(new AttackViewModel { Id = 1, Version = 1, Row = row, Column = column }));

    [Fact]
    public void InputModelsRejectMissingUserDirectionAndReversedDates()
    {
        Assert.False(Validate(new NewRequestViewModel()));
        Assert.False(Validate(new NewGameViewModel()));
        Assert.False(Validate(new PlaceShipViewModel { Id = 1, Version = 1 }));
        Assert.False(Validate(new FriendPostFilterViewModel { From = new(2026, 10, 20), Until = new(2026, 10, 19) }));
    }

    [Theory]
    [InlineData("https://youtu.be/abcdefghijk", "https://www.youtube-nocookie.com/embed/abcdefghijk")]
    [InlineData("https://www.youtube.com/watch?v=abcdefghijk", "https://www.youtube-nocookie.com/embed/abcdefghijk")]
    [InlineData("https://youtube.com/shorts/abcdefghijk", "https://www.youtube-nocookie.com/embed/abcdefghijk")]
    [InlineData("https://youtube.com.evil.example/watch?v=abcdefghijk", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("https://youtu.be/<script>", null)]
    public void YouTubeEmbedsAllowOnlyValidatedHostsAndIdentifiers(string input, string? expected)
        => Assert.Equal(expected, SocialDisplay.YouTubeEmbed(input));

    /// <summary>Executes the same DataAnnotations and cross-field validation used by model binding.</summary>
    private static bool Validate(object model)
        => Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);
}
