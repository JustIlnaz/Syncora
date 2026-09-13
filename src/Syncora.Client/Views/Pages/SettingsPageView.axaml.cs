using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Syncora.Client.ViewModels.Pages;
using System;
using System.IO;

namespace Syncora.Client.Views.Pages;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()   
    {
        InitializeComponent();
        ChangeAvatarButton.Click += OnChangeAvatarClick;
    }

    private async void OnChangeAvatarClick(object? sender, EventArgs e)
    {
        if (DataContext is not SettingsPageViewModel viewModel)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите изображение",
            AllowMultiple = false,
            FileTypeFilter =
            [
                FilePickerFileTypes.ImageAll
            ]
        });

        if (files.Count == 0)
            return;

        var file = files[0];
        var buffer = new MemoryStream();
        await using (var source = await file.OpenReadAsync())
        {
            await source.CopyToAsync(buffer);
        }

        buffer.Position = 0;
        await viewModel.UploadAvatarAsync(buffer, file.Name);
    }
}
