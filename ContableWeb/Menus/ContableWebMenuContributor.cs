using ContableWeb.Menus;
using ContableWeb.Localization;
using ContableWeb.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity.Blazor;
using Volo.Abp.SettingManagement.Blazor.Menus;
using Volo.Abp.UI.Navigation;

namespace ContableWeb.Menus;

public class ContableWebMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<ContableWebResource>();

        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                ContableWebMenus.Home,
                l["Menu:Home"],
                "/",
                icon: "fas fa-home",
                order: 1
            )
        );

        //Administration
        var administration = context.Menu.GetAdministration();
        administration.Order = 4;

        /* Example nested menu definition:

        context.Menu.AddItem(
            new ApplicationMenuItem("Menu0", "Menu Level 0")
            .AddItem(new ApplicationMenuItem("Menu0.1", "Menu Level 0.1", url: "/test01"))
            .AddItem(
                new ApplicationMenuItem("Menu0.2", "Menu Level 0.2")
                    .AddItem(new ApplicationMenuItem("Menu0.2.1", "Menu Level 0.2.1", url: "/test021"))
                    .AddItem(new ApplicationMenuItem("Menu0.2.2", "Menu Level 0.2.2")
                        .AddItem(new ApplicationMenuItem("Menu0.2.2.1", "Menu Level 0.2.2.1", "/test0221"))
                        .AddItem(new ApplicationMenuItem("Menu0.2.2.2", "Menu Level 0.2.2.2", "/test0222"))
                    )
                    .AddItem(new ApplicationMenuItem("Menu0.2.3", "Menu Level 0.2.3", url: "/test023"))
                    .AddItem(new ApplicationMenuItem("Menu0.2.4", "Menu Level 0.2.4", url: "/test024")
                        .AddItem(new ApplicationMenuItem("Menu0.2.4.1", "Menu Level 0.2.4.1", "/test0241"))
                )
                .AddItem(new ApplicationMenuItem("Menu0.2.5", "Menu Level 0.2.5", url: "/test025"))
            )
            .AddItem(new ApplicationMenuItem("Menu0.2", "Menu Level 0.2", url: "/test02"))
        );

        */



        //Administration->Identity
        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 1);

        //Administration->Settings
        administration.SetSubItemOrder(SettingManagementMenus.GroupName, 7);

        context.Menu.AddItem(
            new ApplicationMenuItem(
                "BooksStore",
                l["Menu:ContableWeb"],
                icon: "fa fa-book",
                requiredPermissionName: ContableWebPermissions.Books.Default
            ).AddItem(
                new ApplicationMenuItem(
                    "BooksStore.Books",
                    l["Menu:Books"],
                    url: "/books"
                )
            )
        );
        context.Menu.AddItem(
            new ApplicationMenuItem(
                "Menu:Facturacion",
                l["Menu:Facturacion"],
                icon: "fa fa-file-invoice-dollar",
                url: "/facturacion"
            ).AddItem(
                new ApplicationMenuItem(
                    "Menu:Rubros",
                    l["Menu:Rubros"],
                    icon: "fa fa-list",
                    url: "/rubros"
            )));
        
        context.Menu.AddItem(
            new ApplicationMenuItem(
                "Menu:Cobranzas",
                l["Menu:Cobranzas"],
                icon: "fa fa-coins",
                url: "/cobranzas"
            ));
        
        context.Menu.AddItem(
            new ApplicationMenuItem(
                "Menu:Proveedores",
                l["Menu:Proveedores"],
                icon: "fa fa-building",
                url: "/proveedores"
            ));
        
        context.Menu.AddItem(
            new ApplicationMenuItem(
                "Menu:Configuracion",
                l["Menu:Configuracion"],
                icon: "fa fa-cogs",
                url: "/config"
            ));

        return Task.CompletedTask;
    }
}
