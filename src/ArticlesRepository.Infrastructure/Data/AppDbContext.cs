using ArticlesRepository.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArticlesRepository.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            entity.ToTable("articles");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasColumnName("id");
            entity.Property(a => a.Titulo).HasColumnName("titulo").HasMaxLength(300).IsRequired();
            entity.Property(a => a.Autores).HasColumnName("autores").HasMaxLength(500).IsRequired();
            entity.Property(a => a.Resumo).HasColumnName("resumo").IsRequired();
            entity.Property(a => a.PalavrasChave).HasColumnName("palavras_chave").HasMaxLength(300).IsRequired();
            entity.Property(a => a.Area).HasColumnName("area").HasMaxLength(100).IsRequired();
            entity.HasIndex(a => a.Area);
            entity.Property(a => a.PdfS3Key).HasColumnName("pdf_s3_key").HasMaxLength(500);
            entity.Property(a => a.ResumoTraduzido).HasColumnName("resumo_traduzido");
            entity.Property(a => a.StatusProcessamento).HasColumnName("status_processamento")
                .HasMaxLength(30).IsRequired().HasDefaultValue("pendente");
            entity.Property(a => a.CriadoEm).HasColumnName("criado_em").IsRequired();
            entity.Property(a => a.AtualizadoEm).HasColumnName("atualizado_em");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Article>().Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.AtualizadoEm = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
