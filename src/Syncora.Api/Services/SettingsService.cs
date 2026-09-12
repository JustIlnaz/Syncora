using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Settings;
using Syncora.DTO.User;
using Syncora.Models;

namespace Syncora.Services
{
    public class SettingsService
    {
        private readonly SyncoraDbContext _context;

        public SettingsService(SyncoraDbContext context)
        {
            _context = context;
        }

        public async Task<UserSettingsDto?> GetSettingsAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.WorkingHours)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            var calendarIds = await _context.Calendars
                .Where(c => c.OwnerId == userId || c.Members.Any(m => m.UserId == userId))
                .Select(c => c.Id)
                .ToListAsync();

            var calendarSettings = await _context.UserCalendarSettings
                .Where(s => s.UserId == userId && calendarIds.Contains(s.CalendarId))
                .Include(s => s.Calendar)
                .ToListAsync();

            var accessSettings = await _context.UserAccessSettings
                .Where(s => s.UserId == userId && calendarIds.Contains(s.CalendarId))
                .ToListAsync();

            var result = new UserSettingsDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Timezone = user.Timezone,
                WorkingHours = user.WorkingHours
                    .OrderBy(w => w.DayOfWeek)
                    .Select(w => new WorkingHoursDto
                    {
                        Id = w.Id,
                        DayOfWeek = w.DayOfWeek,
                        DayName = GetDayName(w.DayOfWeek),
                        StartTime = w.StartTime,
                        EndTime = w.EndTime,
                        IsWorkingDay = w.IsWorkingDay
                    }).ToList(),
                CalendarSettings = calendarSettings.Select(cs =>
                {
                    var access = accessSettings.FirstOrDefault(a => a.CalendarId == cs.CalendarId);
                    return new CalendarSettingsItemDto
                    {
                        CalendarId = cs.CalendarId,
                        CalendarName = cs.Calendar?.Name ?? "",
                        DefaultView = cs.DefaultView,
                        PrivacyLevel = access?.PrivacyLevel
                    };
                }).ToList()
            };

            return result;
        }

        public async Task<bool> UpdateAccessSettingsAsync(Guid userId, UpdateAccessSettingsRequest request)
        {
            var hasAccess = await _context.Calendars
                .AnyAsync(c => c.Id == request.CalendarId &&
                              (c.OwnerId == userId || c.Members.Any(m => m.UserId == userId)));

            if (!hasAccess) return false;

            var setting = await _context.UserAccessSettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.CalendarId == request.CalendarId);

            if (setting == null)
            {
                setting = new UserAccessSettings
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CalendarId = request.CalendarId,
                    PrivacyLevel = request.PrivacyLevel,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserAccessSettings.Add(setting);
            }
            else
            {
                setting.PrivacyLevel = request.PrivacyLevel;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCalendarSettingsAsync(Guid userId, UpdateCalendarSettingsRequest request)
        {
            var hasAccess = await _context.Calendars
                .AnyAsync(c => c.Id == request.CalendarId &&
                              (c.OwnerId == userId || c.Members.Any(m => m.UserId == userId)));

            if (!hasAccess) return false;

            var setting = await _context.UserCalendarSettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.CalendarId == request.CalendarId);

            if (setting == null)
            {
                setting = new UserCalendarSettings
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CalendarId = request.CalendarId,
                    DefaultView = request.DefaultView,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserCalendarSettings.Add(setting);
            }
            else
            {
                setting.DefaultView = request.DefaultView;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private static string GetDayName(short day) => day switch
        {
            1 => "Понедельник",
            2 => "Вторник",
            3 => "Среда",
            4 => "Четверг",
            5 => "Пятница",
            6 => "Суббота",
            7 => "Воскресенье",
            _ => ""
        };
    }
}
