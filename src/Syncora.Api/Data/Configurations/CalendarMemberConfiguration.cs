using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class CalendarMemberConfiguration : IEntityTypeConfiguration<CalendarMember>
    {
        public void Configure(EntityTypeBuilder<CalendarMember> builder)
        {
            builder.ToTable("calendar_members");
            builder.HasKey(cm => cm.Id);

            builder.Property(cm => cm.Role).HasMaxLength(20);
            builder.Property(cm => cm.AccessLevel).HasMaxLength(20);

            builder.Property(cm => cm.CreatedAt).IsRequired();
            builder.Property(cm => cm.UpdatedAt).IsRequired();

            builder.HasOne(cm => cm.Calendar)
                   .WithMany(c => c.Members)
                   .HasForeignKey(cm => cm.CalendarId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cm => cm.User)
                   .WithMany(u => u.CalendarMemberships)
                   .HasForeignKey(cm => cm.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(cm => new { cm.CalendarId, cm.UserId }).IsUnique();
        }
    }
}
