using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syncora.DTO.Shopping;
using Syncora.Models;
using Syncora.Services;

namespace Syncora.Controllers
{
    [ApiController]
    [Route("api/shopping-lists")]
    [Authorize]
    public class ShoppingListsController : ControllerBase
    {
        private readonly ShoppingListService _shoppingService;

        public ShoppingListsController(ShoppingListService shoppingService)
        {
            _shoppingService = shoppingService;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirst("sub")?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException());

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _shoppingService.GetMyListsAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _shoppingService.GetByIdAsync(id, CurrentUserId);
            if (result == null) return NotFound(new { message = "Список не найден" });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateShoppingListRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _shoppingService.CreateAsync(request, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShoppingListRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _shoppingService.UpdateAsync(id, request, CurrentUserId);
            if (result == null) return NotFound(new { message = "Список не найден или нет прав" });
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _shoppingService.DeleteAsync(id, CurrentUserId);
            if (!ok) return NotFound(new { message = "Список не найден или нет прав" });
            return NoContent();
        }

        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] AddShoppingListMemberRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _shoppingService.AddMemberAsync(id, request, CurrentUserId);
            if (result == null) return NotFound(new { message = "Список не найден, нет прав или пользователь не найден" });
            return Ok(result);
        }

        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            var ok = await _shoppingService.RemoveMemberAsync(id, userId, CurrentUserId);
            if (!ok) return NotFound(new { message = "Участник не найден или нет прав" });
            return NoContent();
        }

        [HttpPost("{id}/items")]
        public async Task<IActionResult> AddItem(Guid id, [FromBody] AddShoppingItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _shoppingService.AddItemAsync(id, request, CurrentUserId);
            if (result == null) return NotFound(new { message = "Список не найден или нет прав" });
            return Ok(result);
        }

        [HttpPut("items/{itemId}")]
        [HttpPut("{id}/items/{itemId}")]
        public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateShoppingItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _shoppingService.UpdateItemAsync(itemId, request, CurrentUserId);
            if (result == null) return NotFound(new { message = "Товар не найден или нет прав" });
            return Ok(result);
        }

        [HttpDelete("items/{itemId}")]
        [HttpDelete("{id}/items/{itemId}")]
        public async Task<IActionResult> DeleteItem(Guid itemId)
        {
            var ok = await _shoppingService.DeleteItemAsync(itemId, CurrentUserId);
            if (!ok) return NotFound(new { message = "Товар не найден или нет прав" });
            return NoContent();
        }
    }
}
