namespace OrderManager.Data;

using Microsoft.EntityFrameworkCore;
using OrderManager.Core.Entities;

/// <summary>
/// EF Core database context for the <see cref="OrderManagerDbContext"/> application.
/// </summary>
public sealed class OrderManagerDbContext : DbContext
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderManagerDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public OrderManagerDbContext(DbContextOptions<OrderManagerDbContext> options)
        : base(options)
    {
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the orders.
    /// </summary>
    public DbSet<Order> Orders { get; set; } = null!;

    /// <summary>
    /// Gets or sets the boards.
    /// </summary>
    public DbSet<Board> Boards { get; set; } = null!;

    /// <summary>
    /// Gets or sets the components.
    /// </summary>
    public DbSet<Component> Components { get; set; } = null!;

    /// <summary>
    /// Gets or sets the order-board join records.
    /// </summary>
    public DbSet<OrderBoard> OrderBoards { get; set; } = null!;

    /// <summary>
    /// Gets or sets the board-component join records.
    /// </summary>
    public DbSet<BoardComponent> BoardComponents { get; set; } = null!;

    #endregion

    #region Protected Methods

    /// <summary>
    /// Configures the entity model for the <see cref="OrderManagerDbContext"/>.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Name).IsRequired();
            entity.Property(o => o.Description).IsRequired();
        });

        modelBuilder.Entity<Board>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Name).IsRequired();
            entity.Property(b => b.Description).IsRequired();
        });

        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired();
            entity.Property(c => c.Description).IsRequired();
        });

        modelBuilder.Entity<Order>()
            .Navigation(o => o.OrderBoards).UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<Board>()
            .Navigation(b => b.BoardComponents).UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<OrderBoard>(entity =>
        {
            entity.HasKey(ob => new { ob.OrderId, ob.BoardId });
            entity.Property(ob => ob.Quantity).IsRequired();

            // Order owns the relationship; Board has no back-navigation
            entity.HasOne(ob => ob.Board).WithMany().HasForeignKey(ob => ob.BoardId);
            entity.HasOne<Order>().WithMany(o => o.OrderBoards).HasForeignKey(ob => ob.OrderId);
        });

        modelBuilder.Entity<BoardComponent>(entity =>
        {
            entity.HasKey(bc => new { bc.BoardId, bc.ComponentId });

            // Board owns the relationship; Component has no back-navigation
            entity.HasOne(bc => bc.Component).WithMany().HasForeignKey(bc => bc.ComponentId);
            entity.HasOne<Board>().WithMany(b => b.BoardComponents).HasForeignKey(bc => bc.BoardId);
        });
    }

    #endregion
}