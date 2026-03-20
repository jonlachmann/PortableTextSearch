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
            builder.Property(x => x.MessageId).IsRequired();
            builder.HasTextSearch(x => x.Email)
                .HasTextSearch(x => x.Name);
        });
    }
}
