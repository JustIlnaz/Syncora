using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Syncora.Models;

namespace Syncora.Data.Configurations
{
    public class ShoppingListMemberConfiguration : IEntityTypeConfiguration<ShoppingListMember>
    {
        public void Configure(EntityTypeBuilder<ShoppingListMember> builder)
        {
            builder.ToTable("shopping_list_members");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Role).HasMaxLength(20);

            builder.Property(m => m.CreatedAt).IsRequired();
            builder.Property(m => m.UpdatedAt).IsRequired();

            builder.HasOne(m => m.ShoppingList)
                   .WithMany(s => s.Members)
                   .HasForeignKey(m => m.ShoppingListId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.User)
                   .WithMany(u => u.ShoppingListMemberships)
                   .HasForeignKey(m => m.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => new { m.ShoppingListId, m.UserId }).IsUnique();
        }
    }
}
