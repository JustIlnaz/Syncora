using Microsoft.EntityFrameworkCore;
using Syncora.Data;
using Syncora.DTO.Auth;
using Syncora.Helpers;
using Syncora.Models;

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
                PasswordHash = request.Password, 
                Timezone = request.Timezone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return BuildResponse(user);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var email = request.Email.ToLower().Trim();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new UnauthorizedAccessException("Неверный email или пароль");

            if (user.PasswordHash != request.Password)
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
