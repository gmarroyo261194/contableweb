using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using ContableWeb.Entities.Books;
using ContableWeb.Entities.Rubros;

namespace ContableWeb.Data;

public class ContableWebDbContext : AbpDbContext<ContableWebDbContext>
{
    public DbSet<Book> Books { get; set; }
    public DbSet<Rubro> Rubros { get; set; }

    private const string DbTablePrefix = "App";
    private const string DbSchema = null;

    public ContableWebDbContext(DbContextOptions<ContableWebDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigurePermissionManagement();
        builder.ConfigureBlobStoring();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        
        builder.Entity<Book>(b =>
        {
            b.ToTable(DbTablePrefix + "Books",
                DbSchema);
            b.ConfigureByConvention(); //auto configure for the base class props
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        });
        
        builder.Entity<Rubro>(b =>
        {
            b.ToTable(DbTablePrefix + "Rubros",
                DbSchema);
            b.ConfigureByConvention(); 
            b.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
            b.Property(x => x.Enabled).HasDefaultValue(true);
            b.HasIndex(x => x.Nombre).IsUnique();
        });
        
        /* Configure your own entities here */
    }
}

