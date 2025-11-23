using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Biflow.Ui.Core;

public static class PasswordHasher
{
    // Use the default ASP.NET Core Identity PasswordHasher<T> implementation.
    private static readonly PasswordHasher<User> Hasher = new(
        Options.Create(
            new PasswordHasherOptions
            {
                CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
                IterationCount = 100_000
            }));
    
    // Use a dummy user to hash the password.
    // The generic PasswordHasher<T> doesn't access any of the properties of the provided user.
    // The generic type parameter is needed for ASP.NET Core Identity dependency injection,
    // and the user parameter is required for future-proofing (for when the user might be needed).
    private static readonly User DummyUser = new() { Username = "DummyUser" };
    
    public static string Hash(string password) => Hasher.HashPassword(DummyUser, password);

    public static bool Verify(string hash, string password)
    {
        var result = Hasher.VerifyHashedPassword(DummyUser, hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}