using Microsoft.Extensions.Localization;
using ContableWeb.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace ContableWeb;

[Dependency(ReplaceServices = true)]
public class ContableWebBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<ContableWebResource> _localizer;

    public ContableWebBrandingProvider(IStringLocalizer<ContableWebResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
