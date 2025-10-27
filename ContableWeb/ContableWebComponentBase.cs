using ContableWeb.Localization;
using Volo.Abp.AspNetCore.Components;

namespace ContableWeb;

public abstract class ContableWebComponentBase : AbpComponentBase
{
    protected ContableWebComponentBase()
    {
        LocalizationResource = typeof(ContableWebResource);
    }
}
