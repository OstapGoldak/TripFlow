using System.Windows;
using Microsoft.Win32;
using TripFlow.ViewModels;
using TripFlow.Views;

namespace TripFlow.Services;

/// <summary>Діалоги викликає інтерфейс, а не BLL: форма введення та запит підтвердження.</summary>
public interface IDialogService
{
    /// <summary>Показує форму; true — користувач натиснув «Зберегти» і дані коректні.</summary>
    bool ShowForm(FormViewModelBase form);

    bool Confirm(string message);

    /// <summary>Діалог «Зберегти як». Повертає шлях або null, якщо користувач натиснув «Скасувати».</summary>
    string? PickSaveFile(string suggestedFileName, string filter);

    /// <summary>Діалог «Відкрити». Повертає шлях або null, якщо користувач натиснув «Скасувати».</summary>
    string? PickOpenFile(string filter);

    void ShowInfo(string message);

    void ShowError(string message);
}

public sealed class DialogService : IDialogService
{
    public bool ShowForm(FormViewModelBase form) =>
        new FormWindow(form) { Owner = Application.Current.MainWindow }.ShowDialog() == true;

    public bool Confirm(string message) =>
        MessageBox.Show(message, "TripFlow", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public string? PickSaveFile(string suggestedFileName, string filter)
    {
        var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = filter,
            AddExtension = true,
            OverwritePrompt = true
        };
        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
    }

    public string? PickOpenFile(string filter)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true
        };
        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
    }

    public void ShowInfo(string message) =>
        MessageBox.Show(message, "TripFlow", MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string message) =>
        MessageBox.Show(message, "TripFlow", MessageBoxButton.OK, MessageBoxImage.Error);
}
