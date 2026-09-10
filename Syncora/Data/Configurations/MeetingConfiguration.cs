using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
    {
        public void Configure(EntityTypeBuilder<Meeting> builder)
        {
            builder.ToTable("meetings");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Title).HasMaxLength(200).IsRequired();
            builder.Property(m => m.Status).HasMaxLength(20);

            builder.Property(m => m.CreatedAt).IsRequired();
            builder.Property(m => m.UpdatedAt).IsRequired();

            builder.HasOne(m => m.Creator)
                   .WithMany(u => u.CreatedMeetings)
                   .HasForeignKey(m => m.CreatorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(m => m.Participants)
                   .WithOne(p => p.Meeting)
                   .HasForeignKey(p => p.MeetingId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
