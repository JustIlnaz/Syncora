using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syncora.DTO.Settings;
using Syncora.Models;
using Syncora.Services;

namespace Syncora.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly SettingsService _settingsService;

        public SettingsController(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirst("sub")?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException());

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _settingsService.GetSettingsAsync(CurrentUserId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPut("access")]
        public async Task<IActionResult> UpdateAccess([FromBody] UpdateAccessSettingsRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var ok = await _settingsService.UpdateAccessSettingsAsync(CurrentUserId, request);
            if (!ok) return NotFound(new { message = "Календарь не найден или нет доступа" });
            return NoContent();
        }

        [HttpPut("calendar")]
        public async Task<IActionResult> UpdateCalendar([FromBody] UpdateCalendarSettingsRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var ok = await _settingsService.UpdateCalendarSettingsAsync(CurrentUserId, request);
            if (!ok) return NotFound(new { message = "Календарь не найден или нет доступа" });
            return NoContent();
        }
    }
}
