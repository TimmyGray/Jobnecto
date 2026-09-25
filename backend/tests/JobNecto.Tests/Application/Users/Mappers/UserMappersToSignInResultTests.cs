using FluentAssertions;
using JobNecto.Application.Users.Mappers;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;

namespace JobNecto.Tests.Application.Users.Mappers;

public class UserMappersToSignInResultTests
{
    [Fact]
    public void ToSignInResult_NullUser_Throws()
    {
        User? user = null;

        var act = () => user!.ToSignInResult();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToSignInResult_MapsAllFields()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = "daria_dev",
            Email = "daria@example.com",
            Password = "hashed",
            Phone = "+15555550100",
            Location = Location.Ukraine,
            AboutMe = "About Daria",
            Avatar = "https://example.com/avatar.png"
        };

        var result = user.ToSignInResult();

        result.Id.Should().Be(user.Id);
        result.LoginName.Should().Be(user.Login);
        result.Email.Should().Be(user.Email);
        result.Phone.Should().Be(user.Phone);
        result.Location.Should().Be(user.Location.ToString());
        result.About.Should().Be(user.AboutMe);
        result.Avatar.Should().Be(user.Avatar);
    }
}
