using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syncora.DTO.Meeting;
using Syncora.Helpers;
using Syncora.Services;
using System;

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
            if (result == null) return NotFound(ApiError.Body("MEETING_NOT_FOUND", "Встреча не найдена"));
            return Ok(result);
        }

        /// <summary>
        /// Поиск общего свободного времени (ТЗ §18.2): POST /api/meetings/search.
        /// </summary>
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] MeetingSearchRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _searchService.SearchAsync(CurrentUserId, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiError.Body("MEETING_SEARCH_FAILED", ex.Message));
            }
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
            catch (MeetingSlotConflictException ex)
            {
                return Conflict(ApiError.Body("MEETING_SLOT_CONFLICT", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiError.Body("MEETING_CREATE_FAILED", ex.Message));
            }
        }

        [HttpPut("{id}/respond")]
        public async Task<IActionResult> Respond(Guid id, [FromQuery] string status)
        {
            try
            {
                var ok = await _meetingService.RespondAsync(id, CurrentUserId, status);
                if (!ok) return NotFound(ApiError.Body("MEETING_NOT_FOUND", "Встреча не найдена или вы не участник"));
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiError.Body("MEETING_RESPOND_FAILED", ex.Message));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMeetingRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _meetingService.UpdateAsync(
                    id, CurrentUserId, request.Title, request.Description, request.Start, request.End);
                if (result == null) return NotFound(ApiError.Body("MEETING_NOT_FOUND", "Встреча не найдена или нет прав"));
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiError.Body("MEETING_UPDATE_FAILED", ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _meetingService.DeleteAsync(id, CurrentUserId);
            if (!ok) return NotFound(ApiError.Body("MEETING_NOT_FOUND", "Встреча не найдена или нет прав"));
            return NoContent();
        }
    }
}
