// File: ShelfSense.Infrastructure.Services/BCryptPasswordHasher.cs

using BCrypt.Net;
using Org.BouncyCastle.Crypto.Generators;
using ShelfSense.Application.Interfaces;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        // Generates a secure salt and hashes the password with a work factor of 12
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
    }
}