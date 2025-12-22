using FinanceFamilyApp.BLL.Services;
using FinanceFamilyApp.Commands;
using FinanceFamilyApp.Entities;
using System;
using System.Windows;
using System.Windows.Input;

namespace FinanceFamilyApp.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private string _username;
        private string _password;
        private string _errorMessage;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public event Action<User> LoginSuccessful;
        public event Action ShowRegisterRequested;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
            LoginCommand = new RelayCommand(Login, CanLogin);
            RegisterCommand = new RelayCommand(Register);
        }

        private bool CanLogin(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        private async void Login(object parameter)
        {
            try
            {
                var user = await _authService.AuthenticateAsync(Username, Password);
                if (user != null)
                {
                    ErrorMessage = null;
                    LoginSuccessful?.Invoke(user);
                }
                else
                {
                    ErrorMessage = "Неверное имя пользователя или пароль";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка входа: {ex.Message}";
            }
        }

        private void Register(object parameter)
        {
            ShowRegisterRequested?.Invoke();
        }
    }
}