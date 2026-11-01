using EmployeeManagement.Infrastructure.Authentication;
using Xunit;

namespace EmployeeManagement.Tests.Authentication;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ShouldProduceNonEmptyHash()
    {
        var password = "MySecretPassword123!";
        var hash = _hasher.Hash(password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "SecurePassword@2026";
        var hash = _hasher.Hash(password);

        var isValid = _hasher.Verify(password, hash);

        Assert.True(isValid);
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var password = "CorrectPassword1!";
        var hash = _hasher.Hash(password);

        var isValid = _hasher.Verify("WrongPassword2@", hash);

        Assert.False(isValid);
    }
}
