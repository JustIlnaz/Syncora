using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syncora.DTO.Calendar;
using Syncora.Models;
using Syncora.Services;
using Syncora.DTO.Event;

namespace Syncora.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CalendarsController : ControllerBase
    {
        private readonly CalendarService _calendarService;

        public CalendarsController(CalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirst("sub")?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException());

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _calendarService.GetUserCalendarsAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _calendarService.GetByIdAsync(id, CurrentUserId);
            if (result == null) return NotFound(new { message = "Календарь не найден" });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCalendarRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _calendarService.CreateAsync(request, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCalendarRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _calendarService.UpdateAsync(id, request, CurrentUserId);
            if (result == null) return NotFound(new { message = "Календарь не найден или нет прав" });
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _calendarService.DeleteAsync(id, CurrentUserId);
            if (!ok) return NotFound(new { message = "Календарь не найден или нет прав" });
            return NoContent();
        }

        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] AddCalendarMemberRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _calendarService.AddMemberAsync(id, request, CurrentUserId);
                if (result == null) return NotFound(new { message = "Календарь не найден или нет прав" });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/members/{userId}")]
        public async Task<IActionResult> UpdateMember(Guid id, Guid userId, [FromBody] UpdateCalendarMemberRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _calendarService.UpdateMemberAsync(id, userId, request, CurrentUserId);
            if (result == null) return NotFound(new { message = "Участник не найден или нет прав" });
            return Ok(result);
        }

        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            try
            {
                var ok = await _calendarService.RemoveMemberAsync(id, userId, CurrentUserId);
                if (!ok) return NotFound(new { message = "Участник не найден или нет прав" });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
