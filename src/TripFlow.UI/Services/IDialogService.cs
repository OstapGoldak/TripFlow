using System.Windows;
using TripFlow.ViewModels;
using TripFlow.Views;

namespace TripFlow.Services;

/// <summary>Діалоги викликає інтерфейс, а не BLL: форма введення та запит підтвердження.</summary>
public interface IDialogService
{
    /// <summary>Показує форму; true — користувач натиснув «Зберегти» і дані коректні.</summary>
    bool ShowForm(FormViewModelBase form);

    bool Confirm(string message);
}

public sealed class DialogService : IDialogService
{
    public bool ShowForm(FormViewModelBase form) =>
        new FormWindow(form) { Owner = Application.Current.MainWindow }.ShowDialog() == true;

    public bool Confirm(string message) =>
        MessageBox.Show(message, "TripFlow", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
