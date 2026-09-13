using System;
using System.Collections.Generic;

namespace Syncora.Client.Models.Shopping;

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

public class ShoppingItemDto
{
    public Guid Id { get; set; }
    public Guid ShoppingListId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Category { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateShoppingListRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsShared { get; set; }
}

public class UpdateShoppingListRequest
{
    public string? Name { get; set; }
    public bool? IsShared { get; set; }
}

public class AddShoppingItemRequest
{
    public string Name { get; set; } = string.Empty;
    public int? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Category { get; set; }
}

public class UpdateShoppingItemRequest
{
    public string? Name { get; set; }
    public int? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Category { get; set; }
    public bool? IsCompleted { get; set; }
}

public class AddShoppingListMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; } = "member";
}
