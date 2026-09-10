using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
    {
        public void Configure(EntityTypeBuilder<MeetingParticipant> builder)
        {
            builder.ToTable("meeting_participants");
            builder.HasKey(mp => mp.Id);

            builder.Property(mp => mp.Status).HasMaxLength(20);

            builder.Property(mp => mp.CreatedAt).IsRequired();
            builder.Property(mp => mp.UpdatedAt).IsRequired();

            builder.HasOne(mp => mp.Meeting)
                   .WithMany(m => m.Participants)
                   .HasForeignKey(mp => mp.MeetingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(mp => mp.User)
                   .WithMany(u => u.MeetingParticipations)
                   .HasForeignKey(mp => mp.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(mp => new { mp.MeetingId, mp.UserId }).IsUnique();
        }
    }
}
