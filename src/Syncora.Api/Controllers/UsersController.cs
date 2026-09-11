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
    }
}
