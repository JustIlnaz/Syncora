using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syncora.DTO.Meeting;
using Syncora.Models;
using Syncora.Services;

namespace Syncora.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MeetingsController : ControllerBase
    {
        private readonly MeetingService _meetingService;
        private readonly MeetingSearchService _searchService;

        public MeetingsController(MeetingService meetingService, MeetingSearchService searchService)
        {
            _meetingService = meetingService;
            _searchService = searchService;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirst("sub")?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException());

        [HttpGet]
        public async Task<IActionResult> GetMyMeetings()
        {
            var result = await _meetingService.GetMyMeetingsAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _meetingService.GetByIdAsync(id, CurrentUserId);
            if (result == null) return NotFound(new { message = "Встреча не найдена" });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMeetingRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _meetingService.CreateAsync(request, CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("find-slots")]
        public async Task<IActionResult> FindSlots([FromBody] FindMeetingTimeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _searchService.FindAvailableSlotsAsync(CurrentUserId, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/respond")]
        public async Task<IActionResult> Respond(Guid id, [FromQuery] string status)
        {
            try
            {
                var ok = await _meetingService.RespondAsync(id, CurrentUserId, status);
                if (!ok) return NotFound(new { message = "Встреча не найдена или вы не участник" });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/confirm-slot")]
        public async Task<IActionResult> ConfirmSlot(
            Guid id,
            [FromQuery] DateTime slotStart,
            [FromQuery] DateTime slotEnd)
        {
            try
            {
                var result = await _meetingService.ConfirmSlotAsync(id, CurrentUserId, slotStart, slotEnd);
                if (result == null) return NotFound(new { message = "Встреча не найдена или нет прав" });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _meetingService.DeleteAsync(id, CurrentUserId);
            if (!ok) return NotFound(new { message = "Встреча не найдена или нет прав" });
            return NoContent();
        }
    }
}
