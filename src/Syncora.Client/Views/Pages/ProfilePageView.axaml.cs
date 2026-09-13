using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Syncora.Client.ViewModels.Pages;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Syncora.Client.Views.Pages;

public partial class ProfilePageView : UserControl
{
    public ProfilePageView()
    {
        InitializeComponent();
        ChangeAvatarButton.Click += OnChangeAvatarClick;
    }

    private async void OnChangeAvatarClick(object? sender, EventArgs e)
    {
        if (DataContext is not ProfilePageViewModel viewModel)
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
        var fileName = file.Name;

        // Читаем файл в память сразу: stream из StorageProvider может быть
        // недоступен/медленен во время multipart-отправки на сервер.
        var buffer = new MemoryStream();
        await using (var source = await file.OpenReadAsync())
        {
            await source.CopyToAsync(buffer);
        }
        buffer.Position = 0;

        await viewModel.UploadAvatarAsync(buffer, fileName);
    }
}
