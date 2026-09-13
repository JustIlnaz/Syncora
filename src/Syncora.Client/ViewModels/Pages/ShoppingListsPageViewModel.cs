using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncora.Client.Models.Shopping;
using Syncora.Client.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels.Pages;

public partial class ShoppingListsPageViewModel : ViewModelBase
{
    private readonly ShoppingService _shoppingService;

    // ── Список списков ─────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<ShoppingListDto> lists = new();

    [ObservableProperty]
    private ShoppingListDto? selectedList;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string successMessage = string.Empty;

    // ── Создание списка ────────────────────────────────────────────
    [ObservableProperty]
    private bool isCreateListOpen;

    [ObservableProperty]
    private string newListName = string.Empty;

    [ObservableProperty]
    private bool newListIsShared;

    // ── Товары ─────────────────────────────────────────────────────
    [ObservableProperty]
    private string newItemName = string.Empty;

    [ObservableProperty]
    private string newItemQuantity = string.Empty;

    [ObservableProperty]
    private bool isAddItemOpen;

    // ── Добавление участника ────────────────────────────────────────
    [ObservableProperty]
    private bool isAddMemberOpen;

    [ObservableProperty]
    private string newMemberEmail = string.Empty;

    public ShoppingListsPageViewModel(ShoppingService shoppingService)
    {
        _shoppingService = shoppingService;
        _ = LoadListsAsync();
    }

    [RelayCommand]
    private async Task LoadListsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _shoppingService.GetListsAsync();
            Lists = new ObservableCollection<ShoppingListDto>(result);
            SelectedList ??= Lists.FirstOrDefault();
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось загрузить списки покупок."; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void SelectList(ShoppingListDto? list)
    {
        if (list == null) return;
        SelectedList = list;
    }

    // ── Создание списка ────────────────────────────────────────────

    [RelayCommand]
    private void OpenCreateList()
    {
        NewListName = string.Empty;
        NewListIsShared = false;
        IsCreateListOpen = true;
    }

    [RelayCommand]
    private void CloseCreateList() => IsCreateListOpen = false;

    [RelayCommand]
    private async Task CreateListAsync()
    {
        if (string.IsNullOrWhiteSpace(NewListName))
        {
            ErrorMessage = "Введите название списка.";
            return;
        }

        try
        {
            var created = await _shoppingService.CreateListAsync(new CreateShoppingListRequest
            {
                Name = NewListName.Trim(),
                IsShared = NewListIsShared
            });
            Lists.Add(created);
            SelectedList = created;
            IsCreateListOpen = false;
            SuccessMessage = "Список создан.";
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось создать список."; }
    }

    [RelayCommand]
    private async Task DeleteListAsync(ShoppingListDto? list)
    {
        if (list == null) return;
        try
        {
            await _shoppingService.DeleteListAsync(list.Id);
            Lists.Remove(list);
            if (SelectedList?.Id == list.Id)
                SelectedList = Lists.FirstOrDefault();
            SuccessMessage = "Список удалён.";
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
    }

    // ── Товары ─────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAddItem()
    {
        NewItemName = string.Empty;
        NewItemQuantity = string.Empty;
        IsAddItemOpen = true;
    }

    [RelayCommand]
    private void CloseAddItem() => IsAddItemOpen = false;

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (SelectedList == null) return;
        if (string.IsNullOrWhiteSpace(NewItemName))
        {
            ErrorMessage = "Введите название товара.";
            return;
        }

        int? quantity = null;
        if (!string.IsNullOrWhiteSpace(NewItemQuantity) && int.TryParse(NewItemQuantity, out var q))
            quantity = q;

        try
        {
            var item = await _shoppingService.AddItemAsync(SelectedList.Id, new AddShoppingItemRequest
            {
                Name = NewItemName.Trim(),
                Quantity = quantity
            });

            // Перезагружаем список чтобы обновить товары
            var updated = await _shoppingService.GetListAsync(SelectedList.Id);
            var idx = Lists.IndexOf(SelectedList);
            if (idx >= 0)
                Lists[idx] = updated;
            SelectedList = updated;
            IsAddItemOpen = false;
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось добавить товар."; }
    }

    [RelayCommand]
    private async Task ToggleItemAsync(ShoppingItemDto? item)
    {
        if (item == null) return;
        try
        {
            await _shoppingService.UpdateItemAsync(item.Id, new UpdateShoppingItemRequest
            {
                IsCompleted = !item.IsCompleted
            });
            item.IsCompleted = !item.IsCompleted;

            // Обновляем UI
            var idx = Lists.IndexOf(SelectedList!);
            if (idx >= 0)
            {
                var itemIdx = Lists[idx].Items.FindIndex(i => i.Id == item.Id);
                if (itemIdx >= 0)
                    Lists[idx].Items[itemIdx] = item;
            }
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(ShoppingItemDto? item)
    {
        if (item == null || SelectedList == null) return;
        try
        {
            await _shoppingService.DeleteItemAsync(item.Id);
            SelectedList.Items.Remove(item);
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
    }

    // ── Участники ──────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAddMember()
    {
        NewMemberEmail = string.Empty;
        IsAddMemberOpen = true;
    }

    [RelayCommand]
    private void CloseAddMember() => IsAddMemberOpen = false;

    [RelayCommand]
    private async Task AddMemberAsync()
    {
        if (SelectedList == null) return;
        if (string.IsNullOrWhiteSpace(NewMemberEmail))
        {
            ErrorMessage = "Введите email участника.";
            return;
        }

        try
        {
            var updated = await _shoppingService.AddMemberAsync(SelectedList.Id,
                new AddShoppingListMemberRequest { Email = NewMemberEmail.Trim() });

            var idx = Lists.IndexOf(SelectedList);
            if (idx >= 0)
                Lists[idx] = updated;
            SelectedList = updated;
            IsAddMemberOpen = false;
            SuccessMessage = "Участник добавлен.";
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось добавить участника."; }
    }

    [RelayCommand]
    private async Task RemoveMemberAsync(ShoppingListMemberDto? member)
    {
        if (member == null || SelectedList == null) return;
        try
        {
            await _shoppingService.RemoveMemberAsync(SelectedList.Id, member.UserId);
            SelectedList.Members.Remove(member);
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
    }
}
