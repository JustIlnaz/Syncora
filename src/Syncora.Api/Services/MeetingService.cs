using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Meeting;
using Syncora.Models;

namespace Syncora.Services
{
    public class MeetingService
    {
        private readonly SyncoraDbContext _context;

        public MeetingService(SyncoraDbContext context)
        {
            _context = context;
        }

        public async Task<MeetingDto> CreateAsync(CreateMeetingRequest request, Guid creatorId)
        {
            if (request.SearchEnd <= request.SearchStart)
                throw new InvalidOperationException("Дата окончания поиска должна быть позже даты начала");

            var meeting = new Meeting
            {
                Id = Guid.NewGuid(),
                CreatorId = creatorId,
                Title = request.Title,
                DurationMinutes = request.DurationMinutes,
                SearchStart = request.SearchStart,
                SearchEnd = request.SearchEnd,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Meetings.Add(meeting);

            _context.MeetingParticipants.Add(new MeetingParticipant
            {
                Id = Guid.NewGuid(),
                MeetingId = meeting.Id,
                UserId = creatorId,
                Status = "accepted",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            var emails = request.ParticipantEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.ToLower().Trim())
                .Distinct()
                .ToList();

            if (emails.Any())
            {
                var users = await _context.Users
                    .Where(u => emails.Contains(u.Email) && u.Id != creatorId)
                    .ToListAsync();

                foreach (var u in users)
                {
                    _context.MeetingParticipants.Add(new MeetingParticipant
                    {
                        Id = Guid.NewGuid(),
                        MeetingId = meeting.Id,
                        UserId = u.Id,
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });

                    _context.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = u.Id,
                        Type = "meeting_invite",
                        Title = "Приглашение на встречу",
                        Message = $"Вас пригласили на встречу: {meeting.Title}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
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
                participant.Meeting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<MeetingDto?> ConfirmSlotAsync(
            Guid meetingId, Guid userId, DateTime slotStart, DateTime slotEnd)
        {
            var meeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null || meeting.CreatorId != userId) return null;

            if (slotEnd <= slotStart)
                throw new InvalidOperationException("Дата окончания должна быть позже даты начала");

            meeting.SelectedSlotStart = slotStart;
            meeting.SelectedSlotEnd = slotEnd;
            meeting.Status = "scheduled";
            meeting.UpdatedAt = DateTime.UtcNow;

            var participants = await _context.MeetingParticipants
                .Where(p => p.MeetingId == meetingId && p.UserId != userId)
                .Select(p => p.UserId)
                .ToListAsync();

            foreach (var pid in participants)
            {
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = pid,
                    Type = "meeting_scheduled",
                    Title = "Встреча назначена",
                    Message = $"Встреча '{meeting.Title}' назначена на {slotStart:dd.MM.yyyy HH:mm}",
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

            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();
            return true;
        }

        private static MeetingDto MapToDto(Meeting m)
        {
            return new MeetingDto
            {
                Id = m.Id,
                CreatorId = m.CreatorId,
                CreatorName = m.Creator?.Name ?? "",
                Title = m.Title,
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
