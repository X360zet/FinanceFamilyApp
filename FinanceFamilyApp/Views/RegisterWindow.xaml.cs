using FinanceFamilyApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace FinanceFamilyApp.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterWindow()
        {
            InitializeComponent();

            _viewModel = App.ServiceProvider.GetRequiredService<RegisterViewModel>();
            DataContext = _viewModel;

            _viewModel.RegistrationSuccessful += OnRegistrationSuccessful;
            _viewModel.CancelRequested += OnCancelRequested;

            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            ConfirmPasswordBox.PasswordChanged += ConfirmPasswordBox_PasswordChanged;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = PasswordBox.Password;
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
        }

        private void OnRegistrationSuccessful(Entities.User user)
        {
            // Закрываем окно регистрации
            DialogResult = true;
            Close();
        }

        private void OnCancelRequested()
        {
            DialogResult = false;
            Close();
        }
    }
}