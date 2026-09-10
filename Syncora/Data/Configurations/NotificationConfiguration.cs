using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("notifications");
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Type).HasMaxLength(50);
            builder.Property(n => n.Title).HasMaxLength(200);
            builder.Property(n => n.Message);
            builder.Property(n => n.IsRead).IsRequired();

            builder.Property(n => n.CreatedAt).IsRequired();
            builder.Property(n => n.UpdatedAt).IsRequired();

            builder.HasOne(n => n.User)
                   .WithMany(u => u.Notifications)
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(n => new { n.UserId, n.IsRead });
        }
    }
}
