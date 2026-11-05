using System.Windows;
using TripFlow.ViewModels;

namespace TripFlow.Views;

/// <summary>Модальне вікно для будь-якої форми: показує поля за типом ViewModel.</summary>
public partial class FormWindow : Window
{
    public FormWindow(FormViewModelBase viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += saved => DialogResult = saved;
    }
}
