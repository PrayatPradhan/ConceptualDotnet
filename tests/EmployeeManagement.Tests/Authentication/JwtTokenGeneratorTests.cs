using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EmployeeManagement.Domain.Constants;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace EmployeeManagement.Tests.Authentication;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ShouldIncludeExpectedClaims()
    {
        var settings = new JwtSettings
        {
            Secret = "Super_Secret_Testing_Key_At_Least_32_Bytes_Long!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 30
        };

        var generator = new JwtTokenGenerator(Options.Create(settings));
        var user = User.Create("alice", "alice@test.com", "dummyhash", Roles.HrManager);

        var tokenString = generator.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal(settings.Issuer, token.Issuer);
        Assert.Contains(settings.Audience, token.Audiences);
        Assert.Equal(user.Id.ToString(), token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(Roles.HrManager, token.Claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role).Value);
        Assert.Equal("alice@test.com", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }
}
