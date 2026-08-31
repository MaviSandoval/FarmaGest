using Microsoft.EntityFrameworkCore;
using FarmaGest.Dominio;

namespace FarmaGest.Datos.Contexto;

public class FarmaGestDbContext : DbContext
{
    public FarmaGestDbContext(DbContextOptions<FarmaGestDbContext> options)
        : base(options)
    {
    }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ObraSocial> ObrasSociales => Set<ObraSocial>();
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<Afiliado> Afiliados => Set<Afiliado>();
    public DbSet<CoberturaPlan> CoberturaPlanes => Set<CoberturaPlan>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<DetalleReceta> DetallesReceta => Set<DetalleReceta>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ===== Rol =====
        modelBuilder.Entity<Rol>(e =>
        {
            e.ToTable("Rol");
            e.Property(x => x.Id).HasColumnName("id_rol");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(50).IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(200);
            e.Property(x => x.Estado).HasColumnName("estado");
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        // ===== Usuario =====
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuario");
            e.Property(x => x.Id).HasColumnName("id_usuario");
            e.Property(x => x.Dni).HasColumnName("dni").HasMaxLength(15).IsRequired();
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
            e.Property(x => x.Apellido).HasColumnName("apellido").HasMaxLength(80).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(120);
            e.Property(x => x.Contrasena).HasColumnName("contrasena").HasMaxLength(256).IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.RequiereCambioContrasena).HasColumnName("requiere_cambio_contrasena");
            e.Property(x => x.RolId).HasColumnName("id_rol");
            e.HasIndex(x => x.Dni).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Rol).WithMany(r => r.Usuarios).HasForeignKey(x => x.RolId);
        });

        // ===== Categoria =====
        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("CategoriasProductos");
            e.Property(x => x.Id).HasColumnName("id_categoria");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado");
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        // ===== Producto =====
        modelBuilder.Entity<Producto>(e =>
        {
            e.ToTable("Producto");
            e.Property(x => x.Id).HasColumnName("id_producto");
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(150).IsRequired();
            e.Property(x => x.PrecioVenta).HasColumnName("precioVenta").HasColumnType("decimal(10,2)");
            e.Property(x => x.StockVenta).HasColumnName("stockVenta");
            e.Property(x => x.RequiereReceta).HasColumnName("requiereReceta");
            e.Property(x => x.EsMedicamento).HasColumnName("esMedicamento");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.CategoriaId).HasColumnName("id_categoria");
            e.HasOne(x => x.Categoria).WithMany(c => c.Productos).HasForeignKey(x => x.CategoriaId);
        });

        // ===== ObraSocial =====
        modelBuilder.Entity<ObraSocial>(e =>
        {
            e.ToTable("ObraSocial");
            e.Property(x => x.Id).HasColumnName("id_ObraSocial");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
            e.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(20).IsRequired();
            e.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(30);
            e.Property(x => x.Estado).HasColumnName("estado");
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        // ===== Plan =====
        modelBuilder.Entity<Plan>(e =>
        {
            e.ToTable("Planes");
            e.Property(x => x.Id).HasColumnName("idPlan");
            e.Property(x => x.NombrePlan).HasColumnName("nombrePlan").HasMaxLength(100).IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(200);
            e.Property(x => x.ObraSocialId).HasColumnName("id_ObraSocial");
            e.HasIndex(x => new { x.NombrePlan, x.ObraSocialId }).IsUnique();
            e.HasOne(x => x.ObraSocial).WithMany(o => o.Planes).HasForeignKey(x => x.ObraSocialId);
        });

        // ===== Afiliado =====
        modelBuilder.Entity<Afiliado>(e =>
        {
            e.ToTable("Afiliados");
            e.Property(x => x.Id).HasColumnName("id_afiliado");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
            e.Property(x => x.Apellido).HasColumnName("apellido").HasMaxLength(80).IsRequired();
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.PlanId).HasColumnName("idPlan");
            e.HasOne(x => x.Plan).WithMany(p => p.Afiliados).HasForeignKey(x => x.PlanId);
        });

        // ===== CoberturaPlan =====
        modelBuilder.Entity<CoberturaPlan>(e =>
        {
            e.ToTable("CoberturaPlan");
            e.Property(x => x.Id).HasColumnName("id_cobertura");
            e.Property(x => x.PorcentajeCobertura).HasColumnName("porcentajeCobertura").HasColumnType("decimal(5,2)");
            e.Property(x => x.PlanId).HasColumnName("idPlan");
            e.Property(x => x.ProductoId).HasColumnName("id_producto");
            e.HasIndex(x => new { x.PlanId, x.ProductoId }).IsUnique();
            e.HasOne(x => x.Plan).WithMany(p => p.Coberturas).HasForeignKey(x => x.PlanId);
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId);
        });

        // ===== Receta =====
        modelBuilder.Entity<Receta>(e =>
        {
            e.ToTable("Recetas");
            e.Property(x => x.Id).HasColumnName("id_Receta");
            e.Property(x => x.FechaEmision).HasColumnName("fechaEmision");
            e.Property(x => x.FechaVencimiento).HasColumnName("fechaVencimiento");
            e.Property(x => x.MatriculaMedico).HasColumnName("matriculaMedico").HasMaxLength(30).IsRequired();
            e.Property(x => x.NombreMedico).HasColumnName("nombreMedico").HasMaxLength(120).IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.AfiliadoId).HasColumnName("id_afiliado");
            e.HasOne(x => x.Afiliado).WithMany(a => a.Recetas).HasForeignKey(x => x.AfiliadoId);
        });

        // ===== DetalleReceta =====
        modelBuilder.Entity<DetalleReceta>(e =>
        {
            e.ToTable("DetalleReceta");
            e.Property(x => x.Id).HasColumnName("id_detalleReceta");
            e.Property(x => x.Cantidad).HasColumnName("Cantidad");
            e.Property(x => x.RecetaId).HasColumnName("id_Receta");
            e.Property(x => x.ProductoId).HasColumnName("id_producto");
            e.HasIndex(x => new { x.RecetaId, x.ProductoId }).IsUnique();
            e.HasOne(x => x.Receta).WithMany(r => r.Detalles).HasForeignKey(x => x.RecetaId);
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId);
        });

        // ===== Caja =====
        modelBuilder.Entity<Caja>(e =>
        {
            e.ToTable("Caja");
            e.Property(x => x.Id).HasColumnName("id_caja");
            e.Property(x => x.FechaApertura).HasColumnName("fecha_apertura");
            e.Property(x => x.FechaCierre).HasColumnName("fecha_cierre");
            e.Property(x => x.MontoInicial).HasColumnName("monto_inicial").HasColumnType("decimal(10,2)");
            e.Property(x => x.MontoFinal).HasColumnName("monto_final").HasColumnType("decimal(10,2)");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.UsuarioId).HasColumnName("id_usuario");
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        // ===== Venta =====
        modelBuilder.Entity<Venta>(e =>
        {
            e.ToTable("Venta");
            e.Property(x => x.Id).HasColumnName("id_venta");
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.TipoVenta).HasColumnName("tipoVenta").HasMaxLength(30).IsRequired();
            e.Property(x => x.RecetaId).HasColumnName("id_Receta");
            e.Property(x => x.UsuarioId).HasColumnName("id_usuario");
            e.Property(x => x.CajaId).HasColumnName("id_caja");
            e.HasOne(x => x.Receta).WithMany().HasForeignKey(x => x.RecetaId);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Caja).WithMany(c => c.Ventas).HasForeignKey(x => x.CajaId);
        });

        // ===== DetalleVenta =====
        modelBuilder.Entity<DetalleVenta>(e =>
        {
            e.ToTable("DetalleVenta");
            e.Property(x => x.Id).HasColumnName("idDetalleVenta");
            e.Property(x => x.Cantidad).HasColumnName("cantidad");
            e.Property(x => x.PrecioUnitario).HasColumnName("precioUnitario").HasColumnType("decimal(10,2)");
            e.Property(x => x.Descuento).HasColumnName("descuento").HasColumnType("decimal(10,2)");
            e.Property(x => x.ProductoId).HasColumnName("id_producto");
            e.Property(x => x.VentaId).HasColumnName("id_venta");
            e.Property(x => x.CoberturaId).HasColumnName("id_cobertura");
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId);
            e.HasOne(x => x.Venta).WithMany(v => v.Detalles).HasForeignKey(x => x.VentaId);
            e.HasOne(x => x.Cobertura).WithMany().HasForeignKey(x => x.CoberturaId);
        });
    }
}