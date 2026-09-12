using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syncora.Models;
using Syncora.Services;

namespace Syncora.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _notificationService;

        public NotificationsController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirst("sub")?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException());

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool onlyUnread = false)
        {
            var result = await _notificationService.GetMyNotificationsAsync(CurrentUserId, onlyUnread);
            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _notificationService.GetUnreadCountAsync(CurrentUserId);
            return Ok(new { count });
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var ok = await _notificationService.MarkAsReadAsync(id, CurrentUserId);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(CurrentUserId);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _notificationService.DeleteAsync(id, CurrentUserId);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
