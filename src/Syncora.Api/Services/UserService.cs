using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.User;
using Syncora.Models;

namespace Syncora.Services
{
    public class UserService
    {
        private static readonly string[] AllowedAvatarExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxAvatarBytes = 2 * 1024 * 1024;

        private readonly SyncoraDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UserService(SyncoraDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<UserDto?> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.WorkingHours)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;
            return MapToDto(user);
        }

        public async Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateUserRequest request)
        {
            var user = await _context.Users
                .Include(u => u.WorkingHours)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            if (request.Name != null) user.Name = request.Name;
            if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl;
            if (request.Timezone != null) user.Timezone = request.Timezone;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDto(user);
        }

        public async Task<UserDto?> UploadAvatarAsync(Guid userId, IFormFile file)
        {
            if (file.Length == 0)
                throw new InvalidOperationException("Файл аватара пустой.");

            if (file.Length > MaxAvatarBytes)
                throw new InvalidOperationException("Размер аватара не должен превышать 2 МБ.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedAvatarExtensions.Contains(extension))
                throw new InvalidOperationException("Допустимые форматы: JPG, PNG, WEBP.");

            var user = await _context.Users
                .Include(u => u.WorkingHours)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            await DeleteAvatarFileAsync(user.AvatarUrl);

            var uploadsDir = GetAvatarsDirectory();

            var fileName = $"{userId}{extension}";
            var physicalPath = Path.Combine(uploadsDir, fileName);

            await using (var stream = File.Create(physicalPath))
            {
                await file.CopyToAsync(stream);
            }

            user.AvatarUrl = $"/avatars/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(user);
        }

        public async Task<UserDto?> DeleteAvatarAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.WorkingHours)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            await DeleteAvatarFileAsync(user.AvatarUrl);
            user.AvatarUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(user);
        }

        public async Task<List<WorkingHoursDto>> GetWorkingHoursAsync(Guid userId)
        {
            var hours = await _context.UserWorkingHours
                .Where(w => w.UserId == userId)
                .OrderBy(w => w.DayOfWeek)
                .ToListAsync();

            return hours.Select(MapToWorkingHoursDto).ToList();
        }

        public async Task<List<WorkingHoursDto>> UpdateWorkingHoursAsync(Guid userId, UpdateWorkingHoursRequest request)
        {
            var existing = await _context.UserWorkingHours
                .Where(w => w.UserId == userId)
                .ToListAsync();

            _context.UserWorkingHours.RemoveRange(existing);

            var newItems = request.Items.Select(item => new UserWorkingHours
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DayOfWeek = item.DayOfWeek,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                IsWorkingDay = item.IsWorkingDay,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            _context.UserWorkingHours.AddRange(newItems);
            await _context.SaveChangesAsync();

            return newItems.OrderBy(w => w.DayOfWeek).Select(MapToWorkingHoursDto).ToList();
        }

        public async Task<List<ContactDto>> GetContactsAsync(Guid userId)
        {
            var contacts = await _context.Contacts
                .Include(c => c.ContactUser)
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.ContactUser!.Name)
                .ToListAsync();

            return contacts.Select(MapToContactDto).ToList();
        }

        public async Task<ContactDto> AddContactAsync(Guid userId, AddContactRequest request)
        {
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim());

            if (targetUser == null)
                throw new InvalidOperationException("Пользователь с таким email не найден");

            if (targetUser.Id == userId)
                throw new InvalidOperationException("Нельзя добавить себя в контакты");

            var exists = await _context.Contacts
                .AnyAsync(c => c.UserId == userId && c.ContactUserId == targetUser.Id);

            if (exists)
                throw new InvalidOperationException("Контакт уже существует");

            var contact = new Contact
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ContactUserId = targetUser.Id,
                Nickname = request.Nickname,
                Timezone = targetUser.Timezone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            contact.ContactUser = targetUser;
            return MapToContactDto(contact);
        }

        public async Task<bool> RemoveContactAsync(Guid userId, Guid contactId)
        {
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId);

            if (contact == null) return false;

            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAccountAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var hasEvents = await _context.Events.AnyAsync(e => e.CreatorId == userId);
            var hasMeetings = await _context.Meetings.AnyAsync(m => m.CreatorId == userId);
            if (hasEvents || hasMeetings)
            {
                throw new InvalidOperationException(
                    "Нельзя удалить профиль с активными событиями или встречами.");
            }

            var participations = await _context.MeetingParticipants
                .Where(p => p.UserId == userId)
                .ToListAsync();
            _context.MeetingParticipants.RemoveRange(participations);

            var contacts = await _context.Contacts
                .Where(c => c.UserId == userId || c.ContactUserId == userId)
                .ToListAsync();
            _context.Contacts.RemoveRange(contacts);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        private string GetAvatarsDirectory()
        {
            var webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "avatars");
            Directory.CreateDirectory(uploadsDir);
            return uploadsDir;
        }

        private async Task DeleteAvatarFileAsync(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
                return;

            var fileName = Path.GetFileName(avatarUrl);
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var physicalPath = Path.Combine(GetAvatarsDirectory(), fileName);
            if (File.Exists(physicalPath))
            {
                await Task.Run(() => File.Delete(physicalPath));
            }
        }

        private static UserDto MapToDto(User u)
        {
            return new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                Timezone = u.Timezone,
                CreatedAt = u.CreatedAt,
                WorkingHours = u.WorkingHours
                    .OrderBy(w => w.DayOfWeek)
                    .Select(MapToWorkingHoursDto)
                    .ToList()
            };
        }

        private static WorkingHoursDto MapToWorkingHoursDto(UserWorkingHours w)
        {
            return new WorkingHoursDto
            {
                Id = w.Id,
                DayOfWeek = w.DayOfWeek,
                DayName = GetDayName(w.DayOfWeek),
                StartTime = w.StartTime,
                EndTime = w.EndTime,
                IsWorkingDay = w.IsWorkingDay
            };
        }

        private static ContactDto MapToContactDto(Contact c)
        {
            return new ContactDto
            {
                Id = c.Id,
                UserId = c.ContactUserId,
                Name = c.ContactUser?.Name ?? "",
                Email = c.ContactUser?.Email ?? "",
                Nickname = c.Nickname,
                AvatarUrl = c.ContactUser?.AvatarUrl,
                Timezone = c.Timezone,
                CreatedAt = c.CreatedAt
            };
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
            _ => "Неизвестно"
        };
    }
}
