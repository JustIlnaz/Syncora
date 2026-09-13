using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Auth;
using Syncora.Helpers;
using Syncora.Models;
using System.Threading.Tasks;

namespace Syncora.Services
{
    public class AuthService
    {
        private readonly SyncoraDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public AuthService(SyncoraDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
        {
            var email = request.Email.ToLower().Trim();

            var exists = await _context.Users.AnyAsync(u => u.Email == email);
            if (exists)
                throw new InvalidOperationException("Пользователь с таким email уже существует");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Email = email,
                PasswordHash = PasswordHelper.Hash(request.Password),
                Timezone = string.IsNullOrWhiteSpace(request.Timezone) ? "UTC" : request.Timezone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // §5.1: после регистрации автоматически создаётся личный календарь
            var personalCalendar = new Calendar
            {
                Id = Guid.NewGuid(),
                OwnerId = user.Id,
                Name = "Личный",
                Color = "#A78BFA",
                Type = "personal",
                Description = "Личный календарь",
                Timezone = user.Timezone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Calendars.Add(personalCalendar);
            _context.CalendarMembers.Add(new CalendarMember
            {
                Id = Guid.NewGuid(),
                CalendarId = personalCalendar.Id,
                UserId = user.Id,
                Role = "owner",
                AccessLevel = "full",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // §14.2: рабочие часы по умолчанию — Пн-Пт 09:00-18:00
            for (short day = 1; day <= 7; day++)
            {
                var isWorkingDay = day <= 5;
                _context.UserWorkingHours.Add(new UserWorkingHours
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    DayOfWeek = day,
                    StartTime = isWorkingDay ? new TimeSpan(9, 0, 0) : TimeSpan.Zero,
                    EndTime = isWorkingDay ? new TimeSpan(18, 0, 0) : TimeSpan.Zero,
                    IsWorkingDay = isWorkingDay,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return BuildResponse(user);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var email = request.Email.ToLower().Trim();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !PasswordHelper.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Неверный email или пароль");

            return BuildResponse(user);
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        private LoginResponse BuildResponse(User user)
        {
            var (token, expiresAt) = _jwtHelper.GenerateToken(user);

            return new LoginResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Token = token,
                RefreshToken = null,
                ExpiresAt = expiresAt
            };
        }
    }
}
