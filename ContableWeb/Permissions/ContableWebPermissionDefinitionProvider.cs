using ContableWeb.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace ContableWeb.Permissions;

public class ContableWebPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(ContableWebPermissions.GroupName);


        var booksPermission = myGroup.AddPermission(ContableWebPermissions.Books.Default, L("Permission:Books"));
        booksPermission.AddChild(ContableWebPermissions.Books.Create, L("Permission:Books.Create"));
        booksPermission.AddChild(ContableWebPermissions.Books.Edit, L("Permission:Books.Edit"));
        booksPermission.AddChild(ContableWebPermissions.Books.Delete, L("Permission:Books.Delete"));
        
        var rubrosPermission = myGroup.AddPermission(ContableWebPermissions.Rubros.Default, L("Permission:Rubros"));
        rubrosPermission.AddChild(ContableWebPermissions.Rubros.Create, L("Permission:Rubros.Create"));
        rubrosPermission.AddChild(ContableWebPermissions.Rubros.Edit, L("Permission:Rubros.Edit"));
        rubrosPermission.AddChild(ContableWebPermissions.Rubros.Delete, L("Permission:Rubros.Delete"));

        var serviciosPermission = myGroup.AddPermission(ContableWebPermissions.Servicios.Default, L("Permission:Servicios"));
        serviciosPermission.AddChild(ContableWebPermissions.Servicios.Create, L("Permission:Servicios.Create"));
        serviciosPermission.AddChild(ContableWebPermissions.Servicios.Edit, L("Permission:Servicios.Edit"));
        serviciosPermission.AddChild(ContableWebPermissions.Servicios.Delete, L("Permission:Servicios.Delete"));
        
        var tiposComprobantesPermission = myGroup.AddPermission(ContableWebPermissions.TiposComprobantes.Default, L("Permission:TiposComprobantes"));
        tiposComprobantesPermission.AddChild(ContableWebPermissions.TiposComprobantes.Create, L("Permission:TiposComprobantes.Create"));
        tiposComprobantesPermission.AddChild(ContableWebPermissions.TiposComprobantes.Edit, L("Permission:TiposComprobantes.Edit"));
        tiposComprobantesPermission.AddChild(ContableWebPermissions.TiposComprobantes.Delete, L("Permission:TiposComprobantes.Delete"));
      
        
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ContableWebResource>(name);
    }
}
