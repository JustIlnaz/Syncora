using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
            builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
            builder.HasIndex(u => u.Email).IsUnique(); 

            builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            builder.Property(u => u.Timezone).HasMaxLength(50);
            builder.Property(u => u.AvatarUrl).HasMaxLength(255);

            builder.Property(u => u.CreatedAt).IsRequired();
            builder.Property(u => u.UpdatedAt).IsRequired();

            builder.HasMany(u => u.WorkingHours)
                   .WithOne(w => w.User)
                   .HasForeignKey(w => w.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.OwnedCalendars)
                   .WithOne(c => c.Owner)
                   .HasForeignKey(c => c.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.CreatedEvents)
                   .WithOne(e => e.Creator)
                   .HasForeignKey(e => e.CreatorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.CreatedMeetings)
                   .WithOne(m => m.Creator)
                   .HasForeignKey(m => m.CreatorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.OwnedShoppingLists)
                   .WithOne(s => s.Owner)
                   .HasForeignKey(s => s.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
