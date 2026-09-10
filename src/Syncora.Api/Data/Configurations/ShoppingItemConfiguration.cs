using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class ShoppingItemConfiguration : IEntityTypeConfiguration<ShoppingItem>
    {
        public void Configure(EntityTypeBuilder<ShoppingItem> builder)
        {
            builder.ToTable("shopping_items");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Name).HasMaxLength(100).IsRequired();
            builder.Property(i => i.Unit).HasMaxLength(50);
            builder.Property(i => i.Category).HasMaxLength(50);
            builder.Property(i => i.IsCompleted).IsRequired();

            builder.Property(i => i.CreatedAt).IsRequired();
            builder.Property(i => i.UpdatedAt).IsRequired();

            builder.HasOne(i => i.ShoppingList)
                   .WithMany(s => s.Items)
                   .HasForeignKey(i => i.ShoppingListId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
