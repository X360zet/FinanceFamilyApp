using FinanceFamilyApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FinanceFamilyApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();

            _viewModel = App.ServiceProvider.GetRequiredService<LoginViewModel>();
            DataContext = _viewModel;

            _viewModel.LoginSuccessful += OnLoginSuccessful;
            _viewModel.ShowRegisterRequested += ShowRegisterWindow;

            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = PasswordBox.Password;
        }

        private void OnLoginSuccessful(Entities.User user)
        {
            try
            {
                // Открываем главное окно
                var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
                var mainViewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();

                // Устанавливаем текущего пользователя (синхронно)
                mainViewModel.SetCurrentUser(user);

                // Устанавливаем DataContext главного окна
                mainWindow.DataContext = mainViewModel;

                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowRegisterWindow()
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Owner = this;
            registerWindow.ShowDialog();
        }
    }
}