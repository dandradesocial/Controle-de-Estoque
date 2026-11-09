using ControleDeEstoque.Models;
using Microsoft.EntityFrameworkCore;

namespace ControleDeEstoque.Data;

public class AppDbContext : DbContext
{
    private readonly string _connectionString;

    public DbSet<Produto> Produtos { get; set; } = null!;
    public DbSet<MovimentacaoEstoque> Movimentacoes { get; set; } = null!;

    public AppDbContext(string connectionString) => _connectionString = connectionString;

    public AppDbContext() : this("Data Source=estoque.db") { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Nome).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Descricao).HasMaxLength(500);
            entity.Property(p => p.Ativo).HasDefaultValue(true);
            entity.HasIndex(p => p.Ativo);   // acelera filtros "Where(p => p.Ativo)"
        });

        modelBuilder.Entity<MovimentacaoEstoque>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Observacao).HasMaxLength(500);

            entity.HasOne(m => m.Produto)
                  .WithMany(p => p.Movimentacoes)
                  .HasForeignKey(m => m.ProdutoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}