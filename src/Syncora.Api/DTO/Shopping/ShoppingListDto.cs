namespace Syncora.DTO.Shopping
{
    public class ShoppingListDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsShared { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ShoppingListMemberDto> Members { get; set; } = new();
        public List<ShoppingItemDto> Items { get; set; } = new();
    }

    public class ShoppingListMemberDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Role { get; set; }
    }
}
