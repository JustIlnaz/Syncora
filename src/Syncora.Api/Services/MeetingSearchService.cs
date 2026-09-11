using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Meeting;

namespace Syncora.Services
{
    public class MeetingSearchService
    {
        private readonly SyncoraDbContext _context;

        public MeetingSearchService(SyncoraDbContext context)
        {
            _context = context;
        }

        public async Task<List<AvailableSlotDto>> FindAvailableSlotsAsync(
            Guid initiatorId,
            FindMeetingTimeRequest request)
        {
            var participantIds = new List<Guid> { initiatorId };

            if (request.ParticipantEmails.Any())
            {
                var emails = request.ParticipantEmails
                    .Select(e => e.ToLower().Trim())
                    .Distinct()
                    .ToList();

                var users = await _context.Users
                    .Where(u => emails.Contains(u.Email))
                    .Select(u => u.Id)
                    .ToListAsync();

                participantIds.AddRange(users);
            }

            participantIds = participantIds.Distinct().ToList();

            var workingHours = await _context.UserWorkingHours
                .Where(w => participantIds.Contains(w.UserId))
                .ToListAsync();

            var busySlots = await GetBusySlotsAsync(participantIds, request.SearchStart, request.SearchEnd);

            var result = new List<AvailableSlotDto>();
            var searchStart = request.SearchStart;
            var searchEnd = request.SearchEnd;

            var currentDate = searchStart.Date;
            var lastDate = searchEnd.Date;

            while (currentDate <= lastDate)
            {
                short dayOfWeek = (short)(currentDate.DayOfWeek == DayOfWeek.Sunday
                    ? 7
                    : (int)currentDate.DayOfWeek);

                var dayWorkingHours = workingHours
                    .Where(w => w.DayOfWeek == dayOfWeek && w.IsWorkingDay)
                    .ToList();

                if (dayWorkingHours.Count == 0 ||
                    dayWorkingHours.Select(w => w.UserId).Distinct().Count() != participantIds.Count)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                var commonStart = dayWorkingHours.Max(w => w.StartTime);
                var commonEnd = dayWorkingHours.Min(w => w.EndTime);

                if (commonStart >= commonEnd)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                var workStart = currentDate.Add(commonStart);
                var workEnd = currentDate.Add(commonEnd);

                if (workStart < searchStart) workStart = searchStart;
                if (workEnd > searchEnd) workEnd = searchEnd;

                if (workStart >= workEnd)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                var freeSlots = SubtractBusySlots(workStart, workEnd, busySlots);

                foreach (var slot in freeSlots)
                {
                    var duration = (int)(slot.End - slot.Start).TotalMinutes;
                    if (duration >= request.DurationMinutes)
                    {
                        result.Add(new AvailableSlotDto
                        {
                            Start = slot.Start,
                            End = slot.End,
                            DurationMinutes = duration
                        });
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            return result
                .OrderBy(s => s.Start)
                .Take(20)
                .ToList();
        }

        private async Task<List<(DateTime Start, DateTime End)>> GetBusySlotsAsync(
            List<Guid> userIds,
            DateTime from,
            DateTime to)
        {
            var calendarIds = await _context.Calendars
                .Where(c => userIds.Contains(c.OwnerId))
                .Select(c => c.Id)
                .ToListAsync();

            var events = await _context.Events
                .Where(e => calendarIds.Contains(e.CalendarId))
                .Where(e => e.StartAt < to && e.EndAt > from)
                .Select(e => new { e.StartAt, e.EndAt })
                .ToListAsync();

            var meetings = await _context.Meetings
                .Where(m => m.Status == "accepted")
                .Where(m => m.SelectedSlotStart != null && m.SelectedSlotEnd != null)
                .Where(m => m.SelectedSlotStart < to && m.SelectedSlotEnd > from)
                .Where(m => m.Participants.Any(p => userIds.Contains(p.UserId)))
                .Select(m => new { Start = m.SelectedSlotStart!.Value, End = m.SelectedSlotEnd!.Value })
                .ToListAsync();

            var busy = new List<(DateTime, DateTime)>();
            busy.AddRange(events.Select(e => (e.StartAt, e.EndAt)));
            busy.AddRange(meetings.Select(m => (m.Start, m.End)));

            return busy;
        }

        private static List<(DateTime Start, DateTime End)> SubtractBusySlots(
            DateTime freeStart,
            DateTime freeEnd,
            List<(DateTime Start, DateTime End)> busy)
        {
            var result = new List<(DateTime Start, DateTime End)>();
            var relevant = busy
                .Where(b => b.Start < freeEnd && b.End > freeStart)
                .OrderBy(b => b.Start)
                .ToList();

            var cursor = freeStart;
            foreach (var b in relevant)
            {
                if (b.Start > cursor)
                {
                    result.Add((cursor, b.Start));
                }
                if (b.End > cursor) cursor = b.End;
                if (cursor >= freeEnd) break;
            }

            if (cursor < freeEnd)
                result.Add((cursor, freeEnd));

            return result;
        }
    }
}
