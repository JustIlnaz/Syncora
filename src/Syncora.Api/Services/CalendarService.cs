using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Calendar;
using Syncora.Models;
using Syncora.Data.Converters;

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
            Syncora.Models.Enums.CalendarType calendarType = Syncora.Models.Enums.CalendarType.Personal;
            if (request.Type != null && !CalendarEnumMapper.TryParseCalendarType(request.Type, out calendarType))
                throw new ArgumentException($"Недопустимый тип календаря: '{request.Type}'. Допустимые значения: personal, work, group");

            var calendar = new Calendar
            {
                Id = Guid.NewGuid(),
                OwnerId = userId,
                Name = request.Name,
                Color = request.Color,
                Type = request.Type == null ? Syncora.Models.Enums.CalendarType.Personal : calendarType,
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
                Role = Syncora.Models.Enums.CalendarRole.Owner,
                AccessLevel = Syncora.Models.Enums.AccessLevel.Full,
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

            var role = Syncora.Models.Enums.CalendarRole.Member;
            if (request.Role != null && !CalendarEnumMapper.TryParseRole(request.Role, out role))
                throw new ArgumentException($"Недопустимая роль: '{request.Role}'. Допустимые значения: owner, member");
            
            var accessLevel = Syncora.Models.Enums.AccessLevel.Edit;
            if (request.AccessLevel != null && !CalendarEnumMapper.TryParseAccessLevel(request.AccessLevel, out accessLevel))
                throw new ArgumentException($"Недопустимый уровень доступа: '{request.AccessLevel}'. Допустимые значения: full, edit, view, free-busy");

            var member = new CalendarMember
            {
                Id = Guid.NewGuid(),
                CalendarId = calendarId,
                UserId = user.Id,
                Role = request.Role == null ? Syncora.Models.Enums.CalendarRole.Member : role,
                AccessLevel = request.AccessLevel == null ? Syncora.Models.Enums.AccessLevel.Edit : accessLevel,
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
                Role = CalendarEnumMapper.RoleToString(member.Role),
                AccessLevel = CalendarEnumMapper.AccessLevelToString(member.AccessLevel)
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

            if (request.Role != null)
            {
                if (!CalendarEnumMapper.TryParseRole(request.Role, out var role))
                    throw new ArgumentException($"Недопустимая роль: '{request.Role}'. Допустимые значения: owner, member");
                member.Role = role;
            }
            if (request.AccessLevel != null)
            {
                if (!CalendarEnumMapper.TryParseAccessLevel(request.AccessLevel, out var accessLevel))
                    throw new ArgumentException($"Недопустимый уровень доступа: '{request.AccessLevel}'. Допустимые значения: full, edit, view, free-busy");
                member.AccessLevel = accessLevel;
            }
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CalendarMemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                UserName = member.User?.Name ?? "",
                Email = member.User?.Email ?? "",
                AvatarUrl = member.User?.AvatarUrl,
                Role = CalendarEnumMapper.RoleToString(member.Role),
                AccessLevel = CalendarEnumMapper.AccessLevelToString(member.AccessLevel)
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
                Type = CalendarEnumMapper.CalendarTypeToString(c.Type),
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
                    Role = CalendarEnumMapper.RoleToString(m.Role),
                    AccessLevel = CalendarEnumMapper.AccessLevelToString(m.AccessLevel)
                }).ToList()
            };
        }
    }
}
