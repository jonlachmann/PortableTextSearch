using Microsoft.EntityFrameworkCore;
using PortableTextSearch.Configuration;

namespace PortableTextSearch.Tests.TestModel;

public sealed class PortableTextSearchTestContext(DbContextOptions<PortableTextSearchTestContext> options) : DbContext(options)
{
    public DbSet<MessageRecipient> MessageRecipients => Set<MessageRecipient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageRecipient>(builder =>
        {
            builder.ToTable("MessageRecipients");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MessageId).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.Name).HasMaxLength(256);
            builder.HasTextSearch(x => x.Email)
                .HasTextSearch(x => x.Name);
        });
    }
}
