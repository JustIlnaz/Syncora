using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syncora.DTO.User;
using Syncora.Models;
using Syncora.Services;

namespace Syncora.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirst("sub")?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException());

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var result = await _userService.GetProfileAsync(CurrentUserId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _userService.UpdateProfileAsync(CurrentUserId, request);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("me/avatar")]
        [RequestSizeLimit(2_097_152)]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Файл аватара не передан." });

            try
            {
                var result = await _userService.UploadAvatarAsync(CurrentUserId, file);
                if (result == null) return NotFound(new { message = "Профиль не найден" });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("me/avatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            var result = await _userService.DeleteAvatarAsync(CurrentUserId);
            if (result == null) return NotFound(new { message = "Профиль не найден" });
            return Ok(result);
        }

        [HttpGet("me/working-hours")]
        public async Task<IActionResult> GetWorkingHours()
        {
            var result = await _userService.GetWorkingHoursAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpPut("me/working-hours")]
        public async Task<IActionResult> UpdateWorkingHours([FromBody] UpdateWorkingHoursRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _userService.UpdateWorkingHoursAsync(CurrentUserId, request);
            return Ok(result);
        }

        [HttpGet("me/contacts")]
        public async Task<IActionResult> GetContacts()
        {
            var result = await _userService.GetContactsAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpPost("me/contacts")]
        public async Task<IActionResult> AddContact([FromBody] AddContactRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _userService.AddContactAsync(CurrentUserId, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("me/contacts/{contactId}")]
        public async Task<IActionResult> RemoveContact(Guid contactId)
        {
            var ok = await _userService.RemoveContactAsync(CurrentUserId, contactId);
            if (!ok) return NotFound(new { message = "Контакт не найден" });
            return NoContent();
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMe()
        {
            try
            {
                var ok = await _userService.DeleteAccountAsync(CurrentUserId);
                if (!ok) return NotFound(new { message = "Профиль не найден" });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
