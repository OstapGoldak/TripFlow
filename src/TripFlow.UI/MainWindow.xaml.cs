using System.Windows;
using TripFlow.ViewModels;

namespace TripFlow;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
