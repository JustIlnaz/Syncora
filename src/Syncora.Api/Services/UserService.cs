using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.User;
using Syncora.Models;

namespace Syncora.Services
{
    public class UserService
    {
        private readonly SyncoraDbContext _context;

        public UserService(SyncoraDbContext context)
        {
            _context = context;
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
