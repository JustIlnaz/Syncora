using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class UserAccessSettingsConfiguration : IEntityTypeConfiguration<UserAccessSettings>
    {
        public void Configure(EntityTypeBuilder<UserAccessSettings> builder)
        {
            builder.ToTable("user_access_settings");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.PrivacyLevel).HasMaxLength(20);

            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.UpdatedAt).IsRequired();

            builder.HasOne(s => s.User)
                   .WithMany(u => u.AccessSettings)
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Calendar)
                   .WithMany(c => c.AccessSettings)
                   .HasForeignKey(s => s.CalendarId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => new { s.UserId, s.CalendarId }).IsUnique();
        }
    }
}
