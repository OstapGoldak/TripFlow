using CommunityToolkit.Mvvm.Input;

namespace TripFlow.ViewModels;

/// <summary>
/// Спільна основа форм-діалогів: заголовок, команда «Зберегти» і перевірка введених даних.
/// Форма закривається лише тоді, коли дані пройшли перевірку (альтернативні потоки зі сценаріїв).
/// </summary>
public abstract partial class FormViewModelBase : ViewModelBase
{
    /// <summary>Повідомляє вікно, що форму можна закрити (true — збережено).</summary>
    public event Action<bool>? CloseRequested;

    public abstract string Title { get; }

    /// <summary>Перевіряє поля. У разі помилки записує текст у ErrorMessage й повертає false.</summary>
    protected abstract bool Validate();

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;
        if (Validate())
        {
            CloseRequested?.Invoke(true);
        }
    }
}
