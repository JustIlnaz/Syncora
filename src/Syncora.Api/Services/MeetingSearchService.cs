using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Meeting;
using Syncora.Services.MeetingEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Syncora.Services
{
    public class MeetingSearchService
    {
        private readonly SyncoraDbContext _context;

        public MeetingSearchService(SyncoraDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Поиск общего свободного времени по контракту ТЗ §18.2.
        /// Инициатор всегда включается в список участников.
        /// </summary>
        public async Task<MeetingSearchResponse> SearchAsync(Guid initiatorId, MeetingSearchRequest request)
        {
            ValidateRange(request.From, request.To);

            var participantIds = request.ParticipantIds
                .Where(id => id != Guid.Empty)
                .Append(initiatorId)
                .Distinct()
                .ToList();

            if (participantIds.Count > 10)
                throw new InvalidOperationException("Во встрече может быть не более 10 участников (§6.2)");

            await EnsureParticipantsExistAsync(participantIds);

            var workingDays = await LoadWorkingDaysAsync(participantIds);
            var busy = await LoadBusyIntervalsAsync(participantIds, request.From, request.To);

            var slots = MeetingSlotFinder.FindCommonSlots(
                workingDays, busy, request.From, request.To, request.DurationMinutes);

            return new MeetingSearchResponse
            {
                Slots = slots
                    .Select(s => new MeetingSlotDto { Start = s.Start, End = s.End })
                    .ToList()
            };
        }

        /// <summary>
        /// Повторная проверка занятости интервала (ТЗ §10.2) — вызывается перед созданием встречи.
        /// </summary>
        public async Task EnsureSlotAvailableAsync(
            Guid initiatorId,
            IReadOnlyCollection<Guid> participantIds,
            DateTime start,
            DateTime end)
        {
            ValidateRange(start, end);

            var ids = participantIds
                .Where(id => id != Guid.Empty)
                .Append(initiatorId)
                .Distinct()
                .ToList();

            var workingDays = await LoadWorkingDaysAsync(ids);
            var busy = await LoadBusyIntervalsAsync(ids, start, end);

            if (!MeetingSlotFinder.IsSlotAvailable(workingDays, busy, start, end))
                throw new MeetingSlotConflictException(
                    "Выбранный интервал больше недоступен: кто-то из участников уже занят.");
        }

        private static void ValidateRange(DateTime from, DateTime to)
        {
            if (to <= from)
                throw new InvalidOperationException("Дата окончания поиска должна быть позже даты начала");
        }

        private async Task EnsureParticipantsExistAsync(List<Guid> participantIds)
        {
            var existingCount = await _context.Users
                .CountAsync(u => participantIds.Contains(u.Id));

            if (existingCount != participantIds.Count)
                throw new InvalidOperationException("Некоторые из указанных участников не найдены");
        }

        private async Task<Dictionary<Guid, IReadOnlyList<WorkingDay>>> LoadWorkingDaysAsync(List<Guid> ids)
        {
            var rows = await _context.UserWorkingHours
                .Where(w => ids.Contains(w.UserId))
                .Select(w => new { w.UserId, w.DayOfWeek, w.StartTime, w.EndTime, w.IsWorkingDay })
                .ToListAsync();

            var map = new Dictionary<Guid, IReadOnlyList<WorkingDay>>();
            foreach (var id in ids)
            {
                var days = rows
                    .Where(r => r.UserId == id)
                    .Select(r => new WorkingDay(r.DayOfWeek, r.StartTime, r.EndTime, r.IsWorkingDay))
                    .ToList();
                map[id] = days;
            }

            return map;
        }

        /// <summary>
        /// Занятость участника = события его календарей (владение ИЛИ членство)
        /// + подтверждённые встречи (scheduled) с его участием.
        /// </summary>
        private async Task<List<BusyInterval>> LoadBusyIntervalsAsync(List<Guid> ids, DateTime from, DateTime to)
        {
            // Календари, где участник — владелец или член (§4: события видны в пределах доступа)
            var calendarIds = await _context.CalendarMembers
                .Where(m => ids.Contains(m.UserId))
                .Select(m => m.CalendarId)
                .Distinct()
                .ToListAsync();

            var ownedCalendarIds = await _context.Calendars
                .Where(c => ids.Contains(c.OwnerId))
                .Select(c => c.Id)
                .ToListAsync();

            calendarIds.AddRange(ownedCalendarIds);
            calendarIds = calendarIds.Distinct().ToList();

            var events = await _context.Events
                .Where(e => calendarIds.Contains(e.CalendarId))
                .Where(e => e.StartAt < to && e.EndAt > from)
                .Select(e => new { e.StartAt, e.EndAt })
                .ToListAsync();

            var meetings = await _context.Meetings
                .Where(m => m.Status == "scheduled")
                .Where(m => m.SelectedSlotStart != null && m.SelectedSlotEnd != null)
                .Where(m => m.SelectedSlotStart < to && m.SelectedSlotEnd > from)
                .Where(m => m.Participants.Any(p => ids.Contains(p.UserId)))
                .Select(m => new { Start = m.SelectedSlotStart!.Value, End = m.SelectedSlotEnd!.Value })
                .ToListAsync();

            var busy = new List<BusyInterval>(events.Count + meetings.Count);
            busy.AddRange(events.Select(e => new BusyInterval(e.StartAt, e.EndAt)));
            busy.AddRange(meetings.Select(m => new BusyInterval(m.Start, m.End)));
            return busy;
        }
    }
}
