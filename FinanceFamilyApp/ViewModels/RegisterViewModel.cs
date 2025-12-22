using FinanceFamilyApp.BLL.Services;
using FinanceFamilyApp.Commands;
using FinanceFamilyApp.Entities;
using System;
using System.Windows;
using System.Windows.Input;

namespace FinanceFamilyApp.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private string _username;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _errorMessage;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<User> RegistrationSuccessful;
        public event Action CancelRequested;

        public RegisterViewModel(AuthService authService)
        {
            _authService = authService;
            RegisterCommand = new RelayCommand(Register, CanRegister);
            CancelCommand = new RelayCommand(Cancel);
        }

        private bool CanRegister(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Password == ConfirmPassword;
        }

        private async void Register(object parameter)
        {
            try
            {
                var result = await _authService.RegisterAsync(Username, Email, Password);
                if (result.IsSuccess)
                {
                    ErrorMessage = null;
                    MessageBox.Show(result.Message, "Успешная регистрация",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Автоматически входим после регистрации
                    var user = await _authService.GetUserByUsernameAsync(Username);
                    if (user != null)
                    {
                        RegistrationSuccessful?.Invoke(user);
                    }
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка регистрации: {ex.Message}";
            }
        }

        private void Cancel(object parameter)
        {
            CancelRequested?.Invoke();
        }
    }
}