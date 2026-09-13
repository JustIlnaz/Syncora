using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Shopping;
using Syncora.Models;

namespace Syncora.Services
{
    public class ShoppingListService
    {
        private readonly SyncoraDbContext _context;

        public ShoppingListService(SyncoraDbContext context)
        {
            _context = context;
        }

        public async Task<List<ShoppingListDto>> GetMyListsAsync(Guid userId)
        {
            var lists = await _context.ShoppingLists
                .Include(s => s.Owner)
                .Include(s => s.Members).ThenInclude(m => m.User)
                .Include(s => s.Items)
                .Where(s => s.OwnerId == userId || s.Members.Any(m => m.UserId == userId))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return lists.Select(MapToDto).ToList();
        }

        public async Task<ShoppingListDto?> GetByIdAsync(Guid listId, Guid userId)
        {
            var list = await _context.ShoppingLists
                .Include(s => s.Owner)
                .Include(s => s.Members).ThenInclude(m => m.User)
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == listId);

            if (list == null) return null;

            var hasAccess = list.OwnerId == userId || list.Members.Any(m => m.UserId == userId);
            if (!hasAccess) return null;

            return MapToDto(list);
        }

        public async Task<ShoppingListDto> CreateAsync(CreateShoppingListRequest request, Guid userId)
        {
            var list = new ShoppingList
            {
                Id = Guid.NewGuid(),
                OwnerId = userId,
                Name = request.Name,
                IsShared = request.IsShared,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ShoppingLists.Add(list);

            _context.ShoppingListMembers.Add(new ShoppingListMember
            {
                Id = Guid.NewGuid(),
                ShoppingListId = list.Id,
                UserId = userId,
                Role = "owner",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            if (request.IsShared && request.MemberEmails.Any())
            {
                var emails = request.MemberEmails
                    .Select(e => e.ToLower().Trim())
                    .Distinct()
                    .ToList();

                var users = await _context.Users
                    .Where(u => emails.Contains(u.Email) && u.Id != userId)
                    .ToListAsync();

                foreach (var u in users)
                {
                    _context.ShoppingListMembers.Add(new ShoppingListMember
                    {
                        Id = Guid.NewGuid(),
                        ShoppingListId = list.Id,
                        UserId = u.Id,
                        Role = "member",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });

                    _context.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = u.Id,
                        Type = "shopping_list_invite",
                        Title = "Новый общий список",
                        Message = $"Вас добавили в список '{list.Name}'",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(list.Id, userId))!;
        }

        public async Task<ShoppingListDto?> UpdateAsync(Guid listId, UpdateShoppingListRequest request, Guid userId)
        {
            var list = await _context.ShoppingLists
                .Include(s => s.Members)
                .FirstOrDefaultAsync(s => s.Id == listId);

            if (list == null) return null;
            if (list.OwnerId != userId) return null;

            if (request.Name != null) list.Name = request.Name;
            if (request.IsShared.HasValue) list.IsShared = request.IsShared.Value;
            list.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(listId, userId);
        }

        public async Task<bool> DeleteAsync(Guid listId, Guid userId)
        {
            var list = await _context.ShoppingLists.FirstOrDefaultAsync(s => s.Id == listId);
            if (list == null || list.OwnerId != userId) return false;

            _context.ShoppingLists.Remove(list);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ShoppingListDto?> AddMemberAsync(Guid listId, AddShoppingListMemberRequest request, Guid userId)
        {
            var list = await _context.ShoppingLists
                .Include(s => s.Members)
                .FirstOrDefaultAsync(s => s.Id == listId);

            if (list == null) return null;
            if (list.OwnerId != userId) return null;

            var email = request.Email.ToLower().Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;

            if (list.Members.Any(m => m.UserId == user.Id))
                return await GetByIdAsync(listId, userId); // already a member

            _context.ShoppingListMembers.Add(new ShoppingListMember
            {
                Id = Guid.NewGuid(),
                ShoppingListId = listId,
                UserId = user.Id,
                Role = request.Role ?? "member",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = "shopping_list_invite",
                Title = "Новый общий список",
                Message = $"Вас добавили в список '{list.Name}'",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return await GetByIdAsync(listId, userId);
        }

        public async Task<bool> RemoveMemberAsync(Guid listId, Guid memberUserId, Guid currentUserId)
        {
            var list = await _context.ShoppingLists
                .Include(s => s.Members)
                .FirstOrDefaultAsync(s => s.Id == listId);

            if (list == null) return false;
            if (list.OwnerId != currentUserId) return false;

            var member = list.Members.FirstOrDefault(m => m.UserId == memberUserId);
            if (member == null) return false;

            // Нельзя удалить владельца
            if (member.UserId == list.OwnerId) return false;

            _context.ShoppingListMembers.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ShoppingItemDto?> AddItemAsync(Guid listId, AddShoppingItemRequest request, Guid userId)
        {
            var list = await _context.ShoppingLists
                .Include(s => s.Members)
                .FirstOrDefaultAsync(s => s.Id == listId);

            if (list == null) return null;

            var hasAccess = list.OwnerId == userId || list.Members.Any(m => m.UserId == userId);
            if (!hasAccess) return null;

            var item = new ShoppingItem
            {
                Id = Guid.NewGuid(),
                ShoppingListId = listId,
                Name = request.Name,
                Quantity = request.Quantity,
                Unit = request.Unit,
                Category = request.Category,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ShoppingItems.Add(item);
            await _context.SaveChangesAsync();

            return MapItemToDto(item);
        }

        public async Task<ShoppingItemDto?> UpdateItemAsync(Guid itemId, UpdateShoppingItemRequest request, Guid userId)
        {
            var item = await _context.ShoppingItems
                .Include(i => i.ShoppingList).ThenInclude(s => s!.Members)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null) return null;

            var list = item.ShoppingList;
            if (list == null) return null;

            var hasAccess = list.OwnerId == userId || list.Members.Any(m => m.UserId == userId);
            if (!hasAccess) return null;

            if (request.Name != null) item.Name = request.Name;
            if (request.Quantity.HasValue) item.Quantity = request.Quantity.Value;
            if (request.Unit != null) item.Unit = request.Unit;
            if (request.Category != null) item.Category = request.Category;
            if (request.IsCompleted.HasValue) item.IsCompleted = request.IsCompleted.Value;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapItemToDto(item);
        }

        public async Task<bool> DeleteItemAsync(Guid itemId, Guid userId)
        {
            var item = await _context.ShoppingItems
                .Include(i => i.ShoppingList).ThenInclude(s => s!.Members)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null) return false;

            var list = item.ShoppingList;
            if (list == null) return false;

            var hasAccess = list.OwnerId == userId || list.Members.Any(m => m.UserId == userId);
            if (!hasAccess) return false;

            _context.ShoppingItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        private static ShoppingListDto MapToDto(ShoppingList s)
        {
            return new ShoppingListDto
            {
                Id = s.Id,
                OwnerId = s.OwnerId,
                OwnerName = s.Owner?.Name ?? "",
                Name = s.Name,
                IsShared = s.IsShared,
                CreatedAt = s.CreatedAt,
                Members = s.Members.Select(m => new ShoppingListMemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    UserName = m.User?.Name ?? "",
                    Email = m.User?.Email ?? "",
                    Role = m.Role
                }).ToList(),
                Items = s.Items.Select(MapItemToDto).ToList()
            };
        }

        private static ShoppingItemDto MapItemToDto(ShoppingItem i)
        {
            return new ShoppingItemDto
            {
                Id = i.Id,
                ShoppingListId = i.ShoppingListId,
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Category = i.Category,
                IsCompleted = i.IsCompleted,
                CreatedAt = i.CreatedAt
            };
        }
    }
}
