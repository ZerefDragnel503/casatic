using CasaticDirectorio.Domain.Entities;
using CasaticDirectorio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CasaticDirectorio.Infrastructure.Data;

/// <summary>
/// DbContext principal de la aplicación.
/// Configura entidades, índices GIN para Full-Text Search y relaciones.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Socio> Socios => Set<Socio>();
    public DbSet<LogActividad> LogsActividad => Set<LogActividad>();
    public DbSet<FormularioContacto> FormulariosContacto => Set<FormularioContacto>();
    public DbSet<Evento> Eventos => Set<Evento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── USUARIO ──────────────────────────────────────────────
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
            e.Property(u => u.TokenRecuperacion).HasMaxLength(500);
            e.Property(u => u.FechaExpiracionToken).HasColumnType("timestamp with time zone");
        });

        // ── SOCIO ────────────────────────────────────────────────
        modelBuilder.Entity<Socio>(e =>
        {
            e.ToTable("socios");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.NombreEmpresa).HasMaxLength(300).IsRequired();
            e.Property(s => s.Slug).HasMaxLength(300).IsRequired();
            e.HasIndex(s => s.Slug).IsUnique();
            e.Property(s => s.Descripcion).HasColumnType("text");
            // Redes sociales migradas a 6 campos separados
            e.Property(s => s.RsWebsite).HasMaxLength(500).HasDefaultValue("");
            e.Property(s => s.RsFacebook).HasMaxLength(500).HasDefaultValue("");
            e.Property(s => s.RsLinkedin).HasMaxLength(500).HasDefaultValue("");
            e.Property(s => s.RsTwitter).HasMaxLength(500).HasDefaultValue("");
            e.Property(s => s.RsInstagram).HasMaxLength(500).HasDefaultValue("");
            e.Property(s => s.RsYoutube).HasMaxLength(500).HasDefaultValue("");
            e.Property(s => s.MarcasRepresenta).HasColumnType("text");
            e.Property(s => s.EstadoFinanciero).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            e.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");

            // Full-Text Search: columna tsvector generada y con índice GIN
            e.Property(s => s.SearchVector)
             .HasColumnType("tsvector")
             .HasComputedColumnSql(
                 "to_tsvector('spanish', coalesce(\"NombreEmpresa\",'') || ' ' || coalesce(\"Descripcion\",''))",
                 stored: true);

            e.HasIndex(s => s.SearchVector)
             .HasMethod("GIN");

            // Almacenar arrays nativos en PostgreSQL
            e.Property(s => s.Especialidades).HasColumnType("text[]");
            e.Property(s => s.Servicios).HasColumnType("text[]");
        });

        // ── LOG ACTIVIDAD ────────────────────────────────────────
        modelBuilder.Entity<LogActividad>(e =>
        {
            e.ToTable("logs_actividad");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(l => l.TipoEvento).HasConversion<string>().HasMaxLength(30);
            e.Property(l => l.Fecha).HasDefaultValueSql("now()");

            e.HasOne(l => l.Socio)
             .WithMany(s => s.Logs)
             .HasForeignKey(l => l.SocioId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(l => l.Fecha);
            e.HasIndex(l => l.TipoEvento);
        });

        // ── FORMULARIO CONTACTO ──────────────────────────────────
        modelBuilder.Entity<FormularioContacto>(e =>
        {
            e.ToTable("formularios_contacto");
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(f => f.Nombre).HasMaxLength(200).IsRequired();
            e.Property(f => f.Correo).HasMaxLength(256).IsRequired();
            e.Property(f => f.Mensaje).HasColumnType("text").IsRequired();
            e.Property(f => f.Fecha).HasDefaultValueSql("now()");

            e.HasOne(f => f.Socio)
             .WithMany(s => s.Formularios)
             .HasForeignKey(f => f.SocioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Eventos ─────────────────────────────────

       modelBuilder.Entity<Evento>(e =>
{
    e.ToTable("eventos");

    e.HasKey(x => x.Id);
    e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

    e.Property(x => x.Titulo)
        .HasMaxLength(300)
        .IsRequired();

    e.Property(x => x.Slug)
        .HasMaxLength(300)
        .IsRequired();

    e.HasIndex(x => x.Slug).IsUnique();

    e.Property(x => x.Descripcion)
        .HasColumnType("text")
        .IsRequired();

    e.Property(x => x.Tipo)
        .HasConversion<string>()
        .HasMaxLength(50);

    e.Property(x => x.Modalidad)
        .HasConversion<string>()
        .HasMaxLength(20);

    e.Property(x => x.FechaInicio)
        .HasColumnType("timestamp with time zone")
        .IsRequired();

    e.Property(x => x.FechaFin)
        .HasColumnType("timestamp with time zone");

    e.Property(x => x.Lugar)
        .HasColumnType("text");

    e.Property(x => x.ImageUrl)
        .HasColumnType("text");

    e.Property(x => x.Estado)
        .HasConversion<string>()
        .HasMaxLength(20)
        .HasDefaultValueSql("'Pendiente'");

    e.Property(x => x.Habilitado)
        .HasDefaultValue(true);

    e.Property(x => x.Destacado)
        .HasDefaultValue(false);

    e.Property(x => x.PublicadoAt)
        .HasColumnType("timestamp with time zone");

    e.Property(x => x.CreatedAt)
        .HasDefaultValueSql("now()");

    // Relaciones
    e.HasOne(x => x.Socio)
        .WithMany(s => s.Eventos)
        .HasForeignKey(x => x.SocioId)
        .OnDelete(DeleteBehavior.Cascade);

    e.HasOne(x => x.Usuario)
        .WithMany()
        .HasForeignKey(x => x.UsuarioId)
        .OnDelete(DeleteBehavior.SetNull);

    // Índices clave
    e.HasIndex(x => x.FechaInicio);
    e.HasIndex(x => x.Estado);
    e.HasIndex(x => x.Destacado);
});
    }
}

       

    