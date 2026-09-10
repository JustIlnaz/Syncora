using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class UserCalendarSettingsConfiguration : IEntityTypeConfiguration<UserCalendarSettings>
    {
        public void Configure(EntityTypeBuilder<UserCalendarSettings> builder)
        {
            builder.ToTable("user_calendar_settings");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.DefaultView).HasMaxLength(20);

            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.UpdatedAt).IsRequired();

            builder.HasOne(s => s.User)
                   .WithMany(u => u.CalendarSettings)
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Calendar)
                   .WithMany(c => c.UserSettings)
                   .HasForeignKey(s => s.CalendarId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => new { s.UserId, s.CalendarId }).IsUnique();
        }
    }
}
