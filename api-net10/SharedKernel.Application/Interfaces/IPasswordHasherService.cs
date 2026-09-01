namespace SharedKernel.Application.Interfaces;

public interface IPasswordHasherService
{
    string HashPassword(string password);
    (bool isValid, bool needsRehash) VerifyPassword(string password, string hashedPassword, string? saltCode, int hashType);
}
