using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syncora.DTO.Event;
using Syncora.Models;
using Syncora.Services;

namespace Syncora.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly EventService _eventService;

        public EventsController(EventService eventService)
        {
            _eventService = eventService;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirst("sub")?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException());

       
        [HttpGet]
        public async Task<IActionResult> GetInRange(
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end)
        {
            var startDate = start ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var endDate = end ?? startDate.AddMonths(1);

            var result = await _eventService.GetEventsInRangeAsync(CurrentUserId, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _eventService.GetByIdAsync(id, CurrentUserId);
            if (result == null) return NotFound(new { message = "Событие не найдено" });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _eventService.CreateAsync(request, CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _eventService.UpdateAsync(id, request, CurrentUserId);
                if (result == null) return NotFound(new { message = "Событие не найдено или нет прав" });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _eventService.DeleteAsync(id, CurrentUserId);
            if (!ok) return NotFound(new { message = "Событие не найдено или нет прав" });
            return NoContent();
        }
    }
}
