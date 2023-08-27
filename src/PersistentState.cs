using Microsoft.AspNetCore.DataProtection;

namespace Hydro;

internal interface IPersistentState
{
    string Protect(string value);
    string Unprotect(string value);
}

internal class PersistentState : IPersistentState
{
    private readonly IDataProtector _protector;

    public PersistentState(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(nameof(PersistentState));
    }

    public string Protect(string value) =>
        _protector.Protect(value);


    public string Unprotect(string value) =>
        _protector.Unprotect(value);
}
