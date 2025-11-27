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
using ContableWeb.Entities.Clientes;
using ContableWeb.Entities.Rubros;
using ContableWeb.Entities.Servicios;
using ContableWeb.Entities.TiposComprobantes;
using ContableWeb.Entities.TiposDocumentos;
using ContableWeb.Entities.TiposCondicionesIva;
using ContableWeb.Entities.Afip;

namespace ContableWeb.Data;

public class ContableWebDbContext : AbpDbContext<ContableWebDbContext>
{
    public DbSet<Book> Books { get; set; } = default!;
    public DbSet<Rubro> Rubros { get; set; } = default!;
    public DbSet<Servicio> Servicios { get; set; } = default!;
    public DbSet<TipoComprobante> TiposComprobantes { get; set; } = default!;
    public DbSet<TipoDocumento> TiposDocumentos { get; set; } = default!;
    public DbSet<TipoCondicionIva> TiposCondicionesIva { get; set; } = default!;
    public DbSet<Cliente> Clientes { get; set; } = default!;
    public DbSet<AfipTokenEntity> AfipTokens { get; set; } = default!;

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
            b.HasQueryFilter(null);
            // Un Rubro tiene muchos Servicios
            b.HasMany(r => r.Servicios)
             .WithOne(s => s.Rubro)
             .HasForeignKey(s => s.RubroId)
             .OnDelete(DeleteBehavior.NoAction);
        });
        
        builder.Entity<Servicio>(b =>
        {
            b.ToTable(DbTablePrefix + "Servicios",
                DbSchema);
            b.ConfigureByConvention(); 
            b.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
            b.Property(x => x.Enabled).HasDefaultValue(true);
            b.HasIndex(x => x.Nombre).IsUnique();
            b.HasOne<Rubro>()
                .WithMany()
                .HasForeignKey(x => x.RubroId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasQueryFilter(null);
        });
        
        builder.Entity<TipoComprobante>(b =>
        {
            b.ToTable(DbTablePrefix + "TiposComprobantes",
                DbSchema);
            b.ConfigureByConvention(); 
            b.Property(x => x.Nombre).IsRequired().HasMaxLength(50);
            b.Property(x => x.Abreviatura).HasMaxLength(5);
            b.Property(x => x.FechaDesde);
            b.Property(x => x.FechaHasta);
            b.Property(x => x.EsFiscal).IsRequired().HasDefaultValue(true);
            b.Property(x => x.Enabled).HasDefaultValue(true);
            b.HasIndex(x => x.Nombre).IsUnique();
            b.HasIndex(x => x.CodigoAfip).IsUnique();
        });
        
        builder.Entity<TipoDocumento>(b =>
        {
            b.ToTable(DbTablePrefix + "TiposDocumentos",
                DbSchema);
            b.ConfigureByConvention(); 
            b.Property(x => x.Descripcion).IsRequired().HasMaxLength(100);
            b.Property(x => x.FechaDesde);
            b.Property(x => x.FechaHasta);
            b.Property(x => x.Enabled).HasDefaultValue(true);
            b.HasIndex(x => x.CodigoAfip).IsUnique();
        });
        
        builder.Entity<TipoCondicionIva>(b =>
        {
            b.ToTable(DbTablePrefix + "TiposCondicionesIva",
                DbSchema);
            b.ConfigureByConvention(); 
            b.Property(x => x.Descripcion).IsRequired().HasMaxLength(100);
            b.Property(x => x.Enabled).HasDefaultValue(true);
            b.HasIndex(x => x.CodigoAfip).IsUnique();
        });
        
        builder.Entity<Cliente>(b =>
        {
            b.ToTable(DbTablePrefix + "Clientes",
                DbSchema);
            b.ConfigureByConvention(); 
            b.Property(x => x.Nombre).IsRequired().HasMaxLength(200);
            b.Property(x => x.TipoDocumento).IsRequired().HasDefaultValue(TipoDoc.DNI);
            b.Property(x => x.NumeroDocumento).IsRequired().HasMaxLength(20);
            b.Property(x => x.Domicilio).HasMaxLength(250);
            b.Property(x => x.Telefono).HasMaxLength(50);
            b.Property(x => x.Email).HasMaxLength(100);
            b.Property(x => x.Observaciones).HasMaxLength(500);
            b.Property(x => x.Enabled).HasDefaultValue(true);
            b.Property(x => x.CondicionIva).IsRequired().HasDefaultValue(TipoCondIva.ConsumidorFinal);
            b.HasIndex(x => new { x.TipoDocumento, x.NumeroDocumento }).IsUnique();
            b.HasIndex(x => x.Nombre);
            b.HasQueryFilter(null);
        });
    }
}
