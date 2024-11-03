using Microsoft.AspNetCore.DataProtection;
using System.IO.Compression;
using System.Text;

namespace Hydro;

internal interface IPersistentState
{
    string Compress(string value);
    string Decompress(string value);
}

internal class PersistentState : IPersistentState
{
    private readonly IDataProtector _protector;

    public PersistentState(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(nameof(PersistentState));
    }

    public string Compress(string value)
    {
        var inputBytes = Encoding.UTF8.GetBytes(value);
        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(outputStream, CompressionMode.Compress))
        {
            brotliStream.Write(inputBytes, 0, inputBytes.Length);
        }
        
        return Convert.ToBase64String(outputStream.ToArray());
    }

    public string Decompress(string value)
    {
        try
        {
            using var memoryStream = new MemoryStream(Convert.FromBase64String(value));
            using var outputStream = new MemoryStream();
            using (var brotliStream = new BrotliStream(memoryStream, CompressionMode.Decompress))
            {
                brotliStream.CopyTo(outputStream);
            }

            return Encoding.UTF8.GetString(outputStream.ToArray());
        }
        catch (FormatException)
        {
            return _protector.Unprotect(value);
        }
    }
}