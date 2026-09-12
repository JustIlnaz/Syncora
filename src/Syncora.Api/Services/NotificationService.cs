using Microsoft.EntityFrameworkCore;
using Syncora.Data;

namespace Syncora.Services
{
    public class NotificationService
    {
        private readonly SyncoraDbContext _context;

        public NotificationService(SyncoraDbContext context)
        {
            _context = context;
        }

        public async Task<List<NotificationDto>> GetMyNotificationsAsync(Guid userId, bool onlyUnread = false)
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);

            if (onlyUnread)
                query = query.Where(n => !n.IsRead);

            var list = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return list.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var n = await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);

            if (n == null) return false;

            n.IsRead = true;
            n.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
                n.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid notificationId, Guid userId)
        {
            var n = await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);

            if (n == null) return false;

            _context.Notifications.Remove(n);
            await _context.SaveChangesAsync();
            return true;
        }
    }

    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
