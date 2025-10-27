using Volo.Abp.Application.Services;
using ContableWeb.Localization;

namespace ContableWeb.Services;

/* Inherit your application services from this class. */
public abstract class ContableWebAppService : ApplicationService
{
    protected ContableWebAppService()
    {
        LocalizationResource = typeof(ContableWebResource);
    }
}