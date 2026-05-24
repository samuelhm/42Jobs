using Microsoft.AspNetCore.DataProtection;

namespace src.Services;

public class EncryptionService
{
    private const string Prefix = "enc:";
    private readonly IDataProtector _protector;

    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("42jobs.api-keys");
    }

    public string? Encrypt(string? plain)
    {
        if (string.IsNullOrWhiteSpace(plain)) return plain;
        return Prefix + _protector.Protect(plain);
    }

    public string? Decrypt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (!value.StartsWith(Prefix)) return value;
        return _protector.Unprotect(value[Prefix.Length..]);
    }
}
