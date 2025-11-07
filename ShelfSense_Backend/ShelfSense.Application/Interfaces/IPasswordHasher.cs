// File: ShelfSense.Application.Interfaces/IPasswordHasher.cs

namespace ShelfSense.Application.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }
}