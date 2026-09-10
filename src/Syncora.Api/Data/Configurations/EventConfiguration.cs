using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("events");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Description);
            builder.Property(e => e.Location).HasMaxLength(200);
            builder.Property(e => e.IsAllDay).IsRequired();

            builder.Property(e => e.StartAt).IsRequired();
            builder.Property(e => e.EndAt).IsRequired();

            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt).IsRequired();

            builder.HasIndex(e => new { e.CalendarId, e.StartAt, e.EndAt });

            builder.HasOne(e => e.Calendar)
                   .WithMany(c => c.Events)
                   .HasForeignKey(e => e.CalendarId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Creator)
                   .WithMany(u => u.CreatedEvents)
                   .HasForeignKey(e => e.CreatorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Recurrence)
        .WithOne()
        .HasForeignKey<Recurrence>(r => r.EventId)
        .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
