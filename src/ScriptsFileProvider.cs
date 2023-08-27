using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Hydro;

internal class ScriptsFileProvider : IFileProvider
{
    private readonly EmbeddedFileProvider _embeddedFileProvider;

    public ScriptsFileProvider(Assembly assembly)
    {
        _embeddedFileProvider = new EmbeddedFileProvider(assembly);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return _embeddedFileProvider.GetDirectoryContents(subpath);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        if (subpath == "/hydro.js")
        {
            subpath = "/Scripts.hydro.js";
        }

        return _embeddedFileProvider.GetFileInfo(subpath);
    }

    public IChangeToken Watch(string filter)
    {
        return _embeddedFileProvider.Watch(filter);
    }
}