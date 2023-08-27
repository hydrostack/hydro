using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;

namespace Hydro;

internal class HydroModelMetadataProvider : DefaultModelMetadataProvider
{
    public HydroModelMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider) : base(detailsProvider)
    {
    }

    public HydroModelMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider, IOptions<MvcOptions> optionsAccessor) : base(detailsProvider, optionsAccessor)
    {
    }

    protected override DefaultMetadataDetails[] CreatePropertyDetails(ModelMetadataIdentity key) =>
        base.CreatePropertyDetails(key)
            .Where(d => d.Key.PropertyInfo?.DeclaringType != typeof(ViewComponent))
            .ToArray();
}