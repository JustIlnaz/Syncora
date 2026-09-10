using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Calendar;
using Syncora.Models;

namespace Syncora.Services
{
    public class CalendarService
    {
        private readonly SyncoraDbContext _context;

        public CalendarService(SyncoraDbContext context)
        {
            _context = context;
        }

        public async Task<List<CalendarDto>> GetUserCalendarsAsync(Guid userId)
        {
            var calendars = await _context.Calendars
                .Include(c => c.Owner)
                .Include(c => c.Members).ThenInclude(m => m.User)
                .Where(c => c.OwnerId == userId || c.Members.Any(m => m.UserId == userId))
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return calendars.Select(MapToDto).ToList();
        }

        public async Task<CalendarDto?> GetByIdAsync(Guid calendarId, Guid userId)
        {
            var calendar = await _context.Calendars
                .Include(c => c.Owner)
                .Include(c => c.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == calendarId);

            if (calendar == null) return null;

            var hasAccess = calendar.OwnerId == userId ||
                            calendar.Members.Any(m => m.UserId == userId);

            if (!hasAccess) return null;

            return MapToDto(calendar);
        }

        public async Task<CalendarDto> CreateAsync(CreateCalendarRequest request, Guid userId)
        {
            var calendar = new Calendar
            {
                Id = Guid.NewGuid(),
                OwnerId = userId,
                Name = request.Name,
                Color = request.Color,
                Type = request.Type,
                Description = request.Description,
                Timezone = request.Timezone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Calendars.Add(calendar);

            _context.CalendarMembers.Add(new CalendarMember
            {
                Id = Guid.NewGuid(),
                CalendarId = calendar.Id,
                UserId = userId,
                Role = "owner",
                AccessLevel = "full",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(calendar.Id, userId))!;
        }

        public async Task<CalendarDto?> UpdateAsync(Guid calendarId, UpdateCalendarRequest request, Guid userId)
        {
            var calendar = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == calendarId);
            if (calendar == null || calendar.OwnerId != userId) return null;

            if (request.Name != null) calendar.Name = request.Name;
            if (request.Color != null) calendar.Color = request.Color;
            if (request.Description != null) calendar.Description = request.Description;
            if (request.Timezone != null) calendar.Timezone = request.Timezone;
            calendar.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(calendarId, userId);
        }

        public async Task<bool> DeleteAsync(Guid calendarId, Guid userId)
        {
            var calendar = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == calendarId);
            if (calendar == null || calendar.OwnerId != userId) return false;

            _context.Calendars.Remove(calendar);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CalendarMemberDto?> AddMemberAsync(Guid calendarId, AddCalendarMemberRequest request, Guid currentUserId)
        {
            var calendar = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == calendarId);
            if (calendar == null || calendar.OwnerId != currentUserId) return null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim());
            if (user == null)
                throw new InvalidOperationException("Пользователь с таким email не найден");

            var exists = await _context.CalendarMembers
                .AnyAsync(m => m.CalendarId == calendarId && m.UserId == user.Id);
            if (exists)
                throw new InvalidOperationException("Пользователь уже является участником календаря");

            var member = new CalendarMember
            {
                Id = Guid.NewGuid(),
                CalendarId = calendarId,
                UserId = user.Id,
                Role = request.Role ?? "member",
                AccessLevel = request.AccessLevel ?? "edit",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CalendarMembers.Add(member);

            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = "calendar_invite",
                Title = "Приглашение в календарь",
                Message = $"Вас добавили в календарь '{calendar.Name}'",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new CalendarMemberDto
            {
                Id = member.Id,
                UserId = user.Id,
                UserName = user.Name,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Role = member.Role,
                AccessLevel = member.AccessLevel
            };
        }

        public async Task<CalendarMemberDto?> UpdateMemberAsync(Guid calendarId, Guid memberUserId, UpdateCalendarMemberRequest request, Guid currentUserId)
        {
            var calendar = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == calendarId);
            if (calendar == null || calendar.OwnerId != currentUserId) return null;

            var member = await _context.CalendarMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.CalendarId == calendarId && m.UserId == memberUserId);
            if (member == null) return null;

            if (request.Role != null) member.Role = request.Role;
            if (request.AccessLevel != null) member.AccessLevel = request.AccessLevel;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CalendarMemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                UserName = member.User?.Name ?? "",
                Email = member.User?.Email ?? "",
                AvatarUrl = member.User?.AvatarUrl,
                Role = member.Role,
                AccessLevel = member.AccessLevel
            };
        }

        public async Task<bool> RemoveMemberAsync(Guid calendarId, Guid memberUserId, Guid currentUserId)
        {
            var calendar = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == calendarId);
            if (calendar == null || calendar.OwnerId != currentUserId) return false;

            if (memberUserId == currentUserId)
                throw new InvalidOperationException("Нельзя удалить владельца календаря");

            var member = await _context.CalendarMembers
                .FirstOrDefaultAsync(m => m.CalendarId == calendarId && m.UserId == memberUserId);
            if (member == null) return false;

            _context.CalendarMembers.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

        private static CalendarDto MapToDto(Calendar c)
        {
            return new CalendarDto
            {
                Id = c.Id,
                OwnerId = c.OwnerId,
                OwnerName = c.Owner?.Name ?? "",
                Name = c.Name,
                Color = c.Color,
                Type = c.Type,
                Description = c.Description,
                Timezone = c.Timezone,
                CreatedAt = c.CreatedAt,
                Members = c.Members.Select(m => new CalendarMemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    UserName = m.User?.Name ?? "",
                    Email = m.User?.Email ?? "",
                    AvatarUrl = m.User?.AvatarUrl,
                    Role = m.Role,
                    AccessLevel = m.AccessLevel
                }).ToList()
            };
        }
    }
}
