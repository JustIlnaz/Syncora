using Syncora.Client.Models.Shopping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Syncora.Client.Services;

public class ShoppingService
{
    private readonly ApiClient _apiClient;

    public ShoppingService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<List<ShoppingListDto>> GetListsAsync()
        => _apiClient.GetAsync<List<ShoppingListDto>>("/api/shopping-lists");

    public Task<ShoppingListDto> GetListAsync(Guid id)
        => _apiClient.GetAsync<ShoppingListDto>($"/api/shopping-lists/{id}");

    public Task<ShoppingListDto> CreateListAsync(CreateShoppingListRequest request)
        => _apiClient.PostAsync<CreateShoppingListRequest, ShoppingListDto>(
            "/api/shopping-lists", request);

    public Task<ShoppingListDto> UpdateListAsync(Guid id, UpdateShoppingListRequest request)
        => _apiClient.PutAsync<UpdateShoppingListRequest, ShoppingListDto>(
            $"/api/shopping-lists/{id}", request);

    public Task DeleteListAsync(Guid id)
        => _apiClient.DeleteAsync($"/api/shopping-lists/{id}");

    public Task<ShoppingItemDto> AddItemAsync(Guid listId, AddShoppingItemRequest request)
        => _apiClient.PostAsync<AddShoppingItemRequest, ShoppingItemDto>(
            $"/api/shopping-lists/{listId}/items", request);

    public Task<ShoppingItemDto> UpdateItemAsync(Guid itemId, UpdateShoppingItemRequest request)
        => _apiClient.PutAsync<UpdateShoppingItemRequest, ShoppingItemDto>(
            $"/api/shopping-lists/items/{itemId}", request);

    public Task DeleteItemAsync(Guid itemId)
        => _apiClient.DeleteAsync($"/api/shopping-lists/items/{itemId}");

    public Task<ShoppingListDto> AddMemberAsync(Guid listId, AddShoppingListMemberRequest request)
        => _apiClient.PostAsync<AddShoppingListMemberRequest, ShoppingListDto>(
            $"/api/shopping-lists/{listId}/members", request);

    public Task RemoveMemberAsync(Guid listId, Guid userId)
        => _apiClient.DeleteAsync($"/api/shopping-lists/{listId}/members/{userId}");
}
