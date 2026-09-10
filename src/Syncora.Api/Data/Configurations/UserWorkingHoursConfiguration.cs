using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class UserWorkingHoursConfiguration : IEntityTypeConfiguration<UserWorkingHours>
    {
        public void Configure(EntityTypeBuilder<UserWorkingHours> builder)
        {
            builder.ToTable("user_working_hours");
            builder.HasKey(w => w.Id);

            builder.Property(w => w.DayOfWeek).IsRequired();
            builder.Property(w => w.StartTime).IsRequired();
            builder.Property(w => w.EndTime).IsRequired();
            builder.Property(w => w.IsWorkingDay).IsRequired();

            builder.Property(w => w.CreatedAt).IsRequired();
            builder.Property(w => w.UpdatedAt).IsRequired();

            builder.HasOne(w => w.User)
                   .WithMany(u => u.WorkingHours)
                   .HasForeignKey(w => w.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
