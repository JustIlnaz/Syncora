using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class ContactConfiguration : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
            builder.ToTable("contacts");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Nickname).HasMaxLength(100);
            builder.Property(c => c.Timezone).HasMaxLength(50);

            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.UpdatedAt).IsRequired();

            builder.HasOne(c => c.User)
                   .WithMany(u => u.Contacts)
                   .HasForeignKey(c => c.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.ContactUser)
                   .WithMany()
                   .HasForeignKey(c => c.ContactUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.UserId, c.ContactUserId }).IsUnique();
        }
    }
}
