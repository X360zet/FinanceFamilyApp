using System.Windows;
using FinanceFamilyApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceFamilyApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Получаем ViewModel через DI и устанавливаем DataContext
            var viewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();
            DataContext = viewModel;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}