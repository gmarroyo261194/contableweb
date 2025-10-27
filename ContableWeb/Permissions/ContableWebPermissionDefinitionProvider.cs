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

        //Define your own permissions here. Example:
        //myGroup.AddPermission(ContableWebPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ContableWebResource>(name);
    }
}
