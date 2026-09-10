using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class CalendarConfiguration : IEntityTypeConfiguration<Calendar>
    {
        public void Configure(EntityTypeBuilder<Calendar> builder)
        {
            builder.ToTable("calendars");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
            builder.Property(c => c.Color).HasMaxLength(20);
            builder.Property(c => c.Type).HasMaxLength(20);
            builder.Property(c => c.Description);
            builder.Property(c => c.Timezone).HasMaxLength(50);

            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.UpdatedAt).IsRequired();

            builder.HasOne(c => c.Owner)
                   .WithMany(u => u.OwnedCalendars)
                   .HasForeignKey(c => c.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Events)
                   .WithOne(e => e.Calendar)
                   .HasForeignKey(e => e.CalendarId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
