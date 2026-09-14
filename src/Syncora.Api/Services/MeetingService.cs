using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Meeting;
using Syncora.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Syncora.Services
{
    public class MeetingService
    {
        private readonly SyncoraDbContext _context;
        private readonly MeetingSearchService _searchService;

        public MeetingService(SyncoraDbContext context, MeetingSearchService searchService)
        {
            _context = context;
            _searchService = searchService;
        }

        /// <summary>
        /// Создание встречи на выбранном слоте (ТЗ §18.3).
        /// Перед сохранением выполняется повторная проверка занятости всех участников (ТЗ §10.2).
        /// </summary>
        public async Task<MeetingDto> CreateAsync(CreateMeetingRequest request, Guid creatorId)
        {
            if (request.End <= request.Start)
                throw new InvalidOperationException("Время окончания встречи должно быть позже начала");

            var participantIds = request.ParticipantIds
                .Where(id => id != Guid.Empty)
                .Append(creatorId)
                .Distinct()
                .ToList();

            if (participantIds.Count < 2)
                throw new InvalidOperationException("Во встрече должно быть минимум 2 участника (§9.1)");
            if (participantIds.Count > 10)
                throw new InvalidOperationException("Во встрече может быть не более 10 участников (§9.1)");

            var existingUsers = await _context.Users
                .Where(u => participantIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            if (existingUsers.Count != participantIds.Count)
                throw new InvalidOperationException("Некоторые из указанных участников не найдены");

            // ТЗ §10.2: слот мог быть занят после поиска — проверяем занятость повторно
            await _searchService.EnsureSlotAvailableAsync(
                creatorId, participantIds, request.Start, request.End);

            var meeting = new Meeting
            {
                Id = Guid.NewGuid(),
                CreatorId = creatorId,
                Title = request.Title,
                Description = request.Description,
                DurationMinutes = (int)(request.End - request.Start).TotalMinutes,
                SearchStart = request.Start,
                SearchEnd = request.End,
                SelectedSlotStart = request.Start,
                SelectedSlotEnd = request.End,
                Status = "scheduled",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Meetings.Add(meeting);

            var now = DateTime.UtcNow;
            foreach (var (userId, user) in existingUsers)
            {
                var isCreator = userId == creatorId;
                _context.MeetingParticipants.Add(new MeetingParticipant
                {
                    Id = Guid.NewGuid(),
                    MeetingId = meeting.Id,
                    UserId = userId,
                    Status = isCreator ? "accepted" : "pending",
                    CreatedAt = now,
                    UpdatedAt = now
                });

                if (!isCreator)
                {
                    _context.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Type = "meeting_invite",
                        Title = "Приглашение на встречу",
                        Message = $"Вас пригласили на встречу: {meeting.Title}",
                        IsRead = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(meeting.Id, creatorId))!;
        }

        public async Task<MeetingDto?> GetByIdAsync(Guid meetingId, Guid userId)
        {
            var meeting = await _context.Meetings
                .Include(m => m.Creator)
                .Include(m => m.Participants).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null) return null;

            var hasAccess = meeting.CreatorId == userId ||
                            meeting.Participants.Any(p => p.UserId == userId);

            if (!hasAccess) return null;

            return MapToDto(meeting);
        }

        public async Task<List<MeetingDto>> GetMyMeetingsAsync(Guid userId)
        {
            var meetings = await _context.Meetings
                .Include(m => m.Creator)
                .Include(m => m.Participants).ThenInclude(p => p.User)
                .Where(m => m.CreatorId == userId || m.Participants.Any(p => p.UserId == userId))
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return meetings.Select(MapToDto).ToList();
        }

        public async Task<bool> RespondAsync(Guid meetingId, Guid userId, string status)
        {
            if (status != "accepted" && status != "declined")
                throw new InvalidOperationException("Статус может быть только accepted или declined");

            var participant = await _context.MeetingParticipants
                .Include(p => p.Meeting)
                .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == userId);

            if (participant == null) return false;

            participant.Status = status;
            participant.UpdatedAt = DateTime.UtcNow;

            if (participant.Meeting != null)
            {
                participant.Meeting.UpdatedAt = DateTime.UtcNow;

                if (status == "accepted")
                    await EnsureCalendarEventsAsync(participant.Meeting);
                else
                    await RemoveCalendarEventForUserAsync(participant.Meeting.Id, userId);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<MeetingDto?> UpdateAsync(
            Guid meetingId, Guid userId,
            string title, string? description, DateTime start, DateTime end)
        {
            var meeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null || meeting.CreatorId != userId) return null;

            if (end <= start)
                throw new InvalidOperationException("Время окончания встречи должно быть позже начала");

            var oldStart = meeting.SelectedSlotStart;
            var oldEnd = meeting.SelectedSlotEnd;

            meeting.Title = title;
            meeting.Description = description;
            meeting.SelectedSlotStart = start;
            meeting.SelectedSlotEnd = end;
            meeting.DurationMinutes = (int)(end - start).TotalMinutes;
            meeting.UpdatedAt = DateTime.UtcNow;

            var calendarEvents = await _context.Events
                .Where(e => e.MeetingId == meetingId)
                .ToListAsync();
            foreach (var calendarEvent in calendarEvents)
            {
                calendarEvent.Title = title;
                calendarEvent.Description = description;
                calendarEvent.StartAt = start;
                calendarEvent.EndAt = end;
                calendarEvent.UpdatedAt = DateTime.UtcNow;
            }

            var participantIds = await _context.MeetingParticipants
                .Where(p => p.MeetingId == meetingId)
                .Select(p => p.UserId)
                .ToListAsync();

            foreach (var pid in participantIds.Where(pid => pid != userId))
            {
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = pid,
                    Type = "meeting_updated",
                    Title = "Встреча изменена",
                    Message = $"Встреча '{title}' перенесена на {start:dd.MM.yyyy HH:mm}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return await GetByIdAsync(meetingId, userId);
        }

        public async Task<bool> DeleteAsync(Guid meetingId, Guid userId)
        {
            var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId);
            if (meeting == null || meeting.CreatorId != userId) return false;

            var participantIds = await _context.MeetingParticipants
                .Where(p => p.MeetingId == meetingId)
                .Select(p => p.UserId)
                .ToListAsync();

            foreach (var pid in participantIds.Where(pid => pid != userId))
            {
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = pid,
                    Type = "meeting_cancelled",
                    Title = "Встреча отменена",
                    Message = $"Встреча '{meeting.Title}' отменена",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            var calendarEvents = await _context.Events
                .Where(e => e.MeetingId == meetingId)
                .ToListAsync();
            _context.Events.RemoveRange(calendarEvents);
            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task EnsureCalendarEventsAsync(Meeting meeting)
        {
            var participantIds = await _context.MeetingParticipants
                .Where(p => p.MeetingId == meeting.Id)
                .Select(p => p.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var participantId in participantIds)
            {
                var calendar = await _context.Calendars
                    .FirstOrDefaultAsync(c => c.OwnerId == participantId && c.Type == "personal");

                if (calendar == null)
                {
                    calendar = new Calendar
                    {
                        Id = Guid.NewGuid(),
                        OwnerId = participantId,
                        Name = "Личный",
                        Color = "#A78BFA",
                        Type = "personal",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Calendars.Add(calendar);
                }

                var exists = await _context.Events
                    .AnyAsync(e => e.MeetingId == meeting.Id && e.CalendarId == calendar.Id);
                if (exists) continue;

                _context.Events.Add(new Event
                {
                    Id = Guid.NewGuid(),
                    CalendarId = calendar.Id,
                    CreatorId = meeting.CreatorId,
                    MeetingId = meeting.Id,
                    Title = meeting.Title,
                    Description = meeting.Description,
                    Color = calendar.Color,
                    StartAt = meeting.SelectedSlotStart!.Value,
                    EndAt = meeting.SelectedSlotEnd!.Value,
                    IsAllDay = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task RemoveCalendarEventForUserAsync(Guid meetingId, Guid userId)
        {
            var events = await _context.Events
                .Where(e => e.MeetingId == meetingId && e.Calendar!.OwnerId == userId)
                .ToListAsync();
            _context.Events.RemoveRange(events);
        }

        private static MeetingDto MapToDto(Meeting m)
        {
            return new MeetingDto
            {
                Id = m.Id,
                CreatorId = m.CreatorId,
                CreatorName = m.Creator?.Name ?? "",
                Title = m.Title,
                Description = m.Description,
                DurationMinutes = m.DurationMinutes,
                SearchStart = m.SearchStart,
                SearchEnd = m.SearchEnd,
                Status = m.Status,
                SelectedSlotStart = m.SelectedSlotStart,
                SelectedSlotEnd = m.SelectedSlotEnd,
                CreatedAt = m.CreatedAt,
                Participants = m.Participants.Select(p => new MeetingParticipantDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    UserName = p.User?.Name ?? "",
                    Email = p.User?.Email ?? "",
                    AvatarUrl = p.User?.AvatarUrl,
                    Status = p.Status
                }).ToList()
            };
        }
    }
}
