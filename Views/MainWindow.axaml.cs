using ARM.Services;
using ARM.ViewModels;
using ARM.Views;
using Avalonia.Controls;

namespace ARM.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var viewModel = new MainViewModel(new PostgresDBService());
            Content = new MainView
            {
                DataContext = viewModel
            };
        }
    }
}

