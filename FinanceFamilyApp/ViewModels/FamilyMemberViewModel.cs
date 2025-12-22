using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FinanceFamilyApp.BLL.Services;
using FinanceFamilyApp.Commands;
using FinanceFamilyApp.Entities;

namespace FinanceFamilyApp.ViewModels
{
    public class FamilyMemberViewModel : ViewModelBase
    {
        private readonly FinanceService _financeService;
        private readonly AuthService _authService;
        private readonly Guid _currentFamilyId;
        private readonly Guid _currentUserId;
        private string _username;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _role = "Пользователь";
        private string _errorMessage;
        private bool _isEditMode;
        private FamilyMember _editingMember;

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    OnPropertyChanged(nameof(IsFormValid));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    OnPropertyChanged(nameof(IsFormValid));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    OnPropertyChanged(nameof(IsFormValid));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (SetProperty(ref _confirmPassword, value))
                {
                    OnPropertyChanged(nameof(IsFormValid));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Role
        {
            get => _role;
            set
            {
                if (SetProperty(ref _role, value))
                {
                    OnPropertyChanged(nameof(IsFormValid));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public bool IsFormValid
        {
            get
            {
                if (IsEditMode)
                {
                    return !string.IsNullOrWhiteSpace(Role);
                }
                else
                {
                    return !string.IsNullOrWhiteSpace(Username) &&
                           !string.IsNullOrWhiteSpace(Email) &&
                           !string.IsNullOrWhiteSpace(Password) &&
                           !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                           Password == ConfirmPassword &&
                           !string.IsNullOrWhiteSpace(Role);
                }
            }
        }

        public ObservableCollection<string> AvailableRoles { get; } = new()
        {
            "Администратор",
            "Пользователь"
        };

        public ICommand AddNewUserCommand { get; }
        public ICommand DeleteFamilyMemberCommand { get; }
        public ICommand CancelCommand { get; }

        public Action CloseAction { get; set; }

        public FamilyMemberViewModel(FinanceService financeService, AuthService authService,
                                     Guid currentFamilyId, Guid currentUserId)
        {
            _financeService = financeService;
            _authService = authService;
            _currentFamilyId = currentFamilyId;
            _currentUserId = currentUserId;
            _isEditMode = false;

            AddNewUserCommand = new RelayCommand(AddNewUser, _ => IsFormValid);
            DeleteFamilyMemberCommand = new RelayCommand(DeleteFamilyMember, _ => IsEditMode);
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        public FamilyMemberViewModel(FinanceService financeService, AuthService authService,
                                     Guid currentFamilyId, Guid currentUserId, FamilyMember existingMember)
        {
            _financeService = financeService;
            _authService = authService;
            _currentFamilyId = currentFamilyId;
            _currentUserId = currentUserId;
            _editingMember = existingMember;
            _isEditMode = true;

            Username = existingMember.User?.Username ?? "";
            Email = existingMember.User?.Email ?? "";
            Role = existingMember.Role;

            AddNewUserCommand = new RelayCommand(UpdateFamilyMember, _ => IsFormValid);
            DeleteFamilyMemberCommand = new RelayCommand(DeleteFamilyMember, _ => IsEditMode);
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private async void AddNewUser(object parameter)
        {
            try
            {
                ErrorMessage = string.Empty;

                var existingUser = await _authService.GetUserByUsernameAsync(Username);
                if (existingUser != null)
                {
                    ErrorMessage = "Пользователь с таким именем уже существует";
                    return;
                }

                var registerResult = await _authService.RegisterFamilyMemberAsync(Username, Email, Password, Role);
                if (!registerResult.IsSuccess)
                {
                    ErrorMessage = registerResult.Message;
                    return;
                }

                var user = await _authService.GetUserByUsernameAsync(Username);
                if (user == null)
                {
                    ErrorMessage = "Ошибка при получении созданного пользователя";
                    return;
                }

                var familyMember = new FamilyMember
                {
                    Id = Guid.NewGuid(),
                    FamilyId = _currentFamilyId,
                    UserId = user.Id,
                    Role = Role,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var addResult = await _financeService.CreateFamilyMemberAsync(familyMember, _currentUserId);
                if (addResult.IsSuccess)
                {
                    MessageBox.Show($"Пользователь {Username} успешно добавлен в семью как {Role}!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    ErrorMessage = addResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при добавлении нового пользователя: {ex.Message}";
            }
        }

        private async void UpdateFamilyMember(object parameter)
        {
            try
            {
                ErrorMessage = string.Empty;

                if (_editingMember != null)
                {
                    _editingMember.Role = Role;

                    var result = await _financeService.ModifyFamilyMemberAsync(_editingMember, _currentUserId);

                    if (result.IsSuccess)
                    {
                        MessageBox.Show($"Роль пользователя {Username} успешно обновлена на '{Role}'!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        CloseWindow();
                    }
                    else
                    {
                        ErrorMessage = result.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при обновлении члена семьи: {ex.Message}";
            }
        }

        private async void DeleteFamilyMember(object parameter)
        {
            if (_editingMember == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя '{Username}' из семьи?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var deleteResult = await _financeService.DeleteFamilyMemberAsync(_editingMember.Id, _currentUserId);

                    if (deleteResult.IsSuccess)
                    {
                        MessageBox.Show(deleteResult.Message, "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        CloseWindow();
                    }
                    else
                    {
                        ErrorMessage = deleteResult.Message;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка при удалении члена семьи: {ex.Message}";
                }
            }
        }

        private void Cancel()
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}