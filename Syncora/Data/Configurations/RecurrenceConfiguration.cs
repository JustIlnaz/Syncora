using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class RecurrenceConfiguration : IEntityTypeConfiguration<Recurrence>
    {
        public void Configure(EntityTypeBuilder<Recurrence> builder)
        {
            builder.ToTable("recurrences");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Frequency).HasMaxLength(20);
            builder.Property(r => r.DayOfWeek).HasMaxLength(20);

            builder.Property(r => r.CreatedAt).IsRequired();
            builder.Property(r => r.UpdatedAt).IsRequired();
        }
    }
}
