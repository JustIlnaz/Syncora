using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Event;
using Syncora.Models;

namespace Syncora.Services
{
    public class EventService
    {
        private readonly SyncoraDbContext _context;

        public EventService(SyncoraDbContext context)
        {
            _context = context;
        }

        public async Task<List<EventDto>> GetEventsInRangeAsync(Guid userId, DateTime start, DateTime end)
        {
            var calendarIds = await _context.Calendars
                .Where(c => c.OwnerId == userId || c.Members.Any(m => m.UserId == userId))
                .Select(c => c.Id)
                .ToListAsync();

            var events = await _context.Events
                .Include(e => e.Calendar)
                .Include(e => e.Creator)
                .Include(e => e.Recurrence)
                .Where(e => calendarIds.Contains(e.CalendarId))
                .Where(e => e.StartAt < end && e.EndAt > start)
                .OrderBy(e => e.StartAt)
                .ToListAsync();

            var acceptedNames = await GetAcceptedParticipantNamesAsync(events);
            return events.Select(e => MapToDto(e, acceptedNames)).ToList();
        }

        public async Task<EventDto?> GetByIdAsync(Guid eventId, Guid userId)
        {
            var ev = await _context.Events
                .Include(e => e.Calendar)
                .Include(e => e.Creator)
                .Include(e => e.Recurrence)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null) return null;

            var hasAccess = await _context.Calendars
                .AnyAsync(c => c.Id == ev.CalendarId &&
                               (c.OwnerId == userId || c.Members.Any(m => m.UserId == userId)));

            if (!hasAccess) return null;

            var acceptedNames = await GetAcceptedParticipantNamesAsync(new List<Event> { ev });
            return MapToDto(ev, acceptedNames);
        }

        /// <summary>Имена участников встреч (accepted), сгруппированные по MeetingId.</summary>
        private async Task<Dictionary<Guid, List<string>>> GetAcceptedParticipantNamesAsync(List<Event> events)
        {
            var meetingIds = events.Where(e => e.MeetingId.HasValue)
                .Select(e => e.MeetingId!.Value).Distinct().ToList();

            var result = new Dictionary<Guid, List<string>>();
            if (meetingIds.Count == 0) return result;

            var names = await _context.MeetingParticipants
                .Where(p => meetingIds.Contains(p.MeetingId) && p.Status == "accepted")
                .Select(p => new { p.MeetingId, Name = p.User != null ? p.User.Name : string.Empty })
                .ToListAsync();

            foreach (var group in names.GroupBy(n => n.MeetingId))
                result[group.Key] = group.Select(n => n.Name).Where(n => n.Length > 0).ToList();

            return result;
        }

        public async Task<EventDto> CreateAsync(CreateEventRequest request, Guid userId)
        {
            var hasAccess = await _context.Calendars
                .AnyAsync(c => c.Id == request.CalendarId &&
                               (c.OwnerId == userId ||
                                c.Members.Any(m => m.UserId == userId && m.AccessLevel != "view")));

            if (!hasAccess)
                throw new UnauthorizedAccessException("Нет доступа к этому календарю");

            if (request.EndAt <= request.StartAt)
                throw new InvalidOperationException("Дата окончания должна быть позже даты начала");

            var hasConflict = await _context.Events
                .AnyAsync(e => e.CalendarId == request.CalendarId &&
                               e.StartAt < request.EndAt &&
                               e.EndAt > request.StartAt);

            if (hasConflict)
                throw new InvalidOperationException("Событие пересекается с другим событием в этом календаре");

            var ev = new Event
            {
                Id = Guid.NewGuid(),
                CalendarId = request.CalendarId,
                CreatorId = userId,
                Title = request.Title,
                Description = request.Description,
                Color = NormalizeColor(request.Color),
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                Location = request.Location,
                IsAllDay = request.IsAllDay,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return (await GetByIdAsync(ev.Id, userId))!;
        }

        public async Task<EventDto?> UpdateAsync(Guid eventId, UpdateEventRequest request, Guid userId)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) return null;

            var calendar = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == ev.CalendarId);
            if (calendar == null) return null;

            var hasAccess = ev.CreatorId == userId || calendar.OwnerId == userId;
            if (!hasAccess) return null;

            if (request.Title != null) ev.Title = request.Title;
            if (request.Description != null) ev.Description = request.Description;
            if (request.Color != null) ev.Color = NormalizeColor(request.Color);
            if (request.StartAt.HasValue) ev.StartAt = request.StartAt.Value;
            if (request.EndAt.HasValue) ev.EndAt = request.EndAt.Value;
            if (request.Location != null) ev.Location = request.Location;
            if (request.IsAllDay.HasValue) ev.IsAllDay = request.IsAllDay.Value;

            if (ev.EndAt <= ev.StartAt)
                throw new InvalidOperationException("Дата окончания должна быть позже даты начала");

            var hasConflict = await _context.Events
                .AnyAsync(e => e.Id != eventId &&
                               e.CalendarId == ev.CalendarId &&
                               e.StartAt < ev.EndAt &&
                               e.EndAt > ev.StartAt);

            if (hasConflict)
                throw new InvalidOperationException("Событие пересекается с другим событием");

            ev.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(eventId, userId);
        }

        public async Task<bool> DeleteAsync(Guid eventId, Guid userId)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) return false;

            var calendar = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == ev.CalendarId);
            if (calendar == null) return false;

            var hasAccess = ev.CreatorId == userId || calendar.OwnerId == userId;
            if (!hasAccess) return false;

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return true;
        }

        private static EventDto MapToDto(Event e, Dictionary<Guid, List<string>>? acceptedNames = null)
        {
            return new EventDto
            {
                Id = e.Id,
                CalendarId = e.CalendarId,
                CalendarName = e.Calendar?.Name ?? "",
                CalendarColor = e.Calendar?.Color,
                CreatorId = e.CreatorId,
                MeetingId = e.MeetingId,
                CreatorName = e.Creator?.Name ?? "",
                Title = e.Title,
                Description = e.Description,
                Color = e.Color,
                StartAt = e.StartAt,
                EndAt = e.EndAt,
                Location = e.Location,
                IsAllDay = e.IsAllDay,
                CreatedAt = e.CreatedAt,
                AcceptedParticipants = acceptedNames != null
                    && e.MeetingId.HasValue
                    && acceptedNames.TryGetValue(e.MeetingId.Value, out var names)
                        ? names
                        : new List<string>(),
                Recurrence = e.Recurrence == null ? null : new RecurrenceDto
                {
                    Id = e.Recurrence.Id,
                    EventId = e.Recurrence.EventId,
                    Frequency = e.Recurrence.Frequency,
                    Interval = e.Recurrence.Interval,
                    DayOfWeek = e.Recurrence.DayOfWeek,
                    EndDate = e.Recurrence.EndDate,
                    Count = e.Recurrence.Count
                }
            };
        }

        private static string? NormalizeColor(string? color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return null;

            return color.Trim();
        }
    }
}
