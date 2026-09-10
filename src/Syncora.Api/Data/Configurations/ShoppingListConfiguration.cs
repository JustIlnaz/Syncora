using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList>
    {
        public void Configure(EntityTypeBuilder<ShoppingList> builder)
        {
            builder.ToTable("shopping_lists");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
            builder.Property(s => s.IsShared).IsRequired();

            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.UpdatedAt).IsRequired();

            builder.HasOne(s => s.Owner)
                   .WithMany(u => u.OwnedShoppingLists)
                   .HasForeignKey(s => s.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Items)
                   .WithOne(i => i.ShoppingList)
                   .HasForeignKey(i => i.ShoppingListId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Members)
                   .WithOne(m => m.ShoppingList)
                   .HasForeignKey(m => m.ShoppingListId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
