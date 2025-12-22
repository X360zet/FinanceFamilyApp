using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FinanceFamilyApp.BLL.DTO;
using FinanceFamilyApp.BLL.Services;
using FinanceFamilyApp.Commands;
using FinanceFamilyApp.Entities;

namespace FinanceFamilyApp.ViewModels
{
    public class BudgetManagementViewModel : ViewModelBase
    {
        private readonly FinanceService _financeService;
        private readonly Guid _currentFamilyId;
        private readonly Guid _currentUserId;
        private BudgetDto _selectedBudget;
        private string _errorMessage = string.Empty;
        private bool _isAdmin;
        private FamilyMember _currentFamilyMember;

        public BudgetDto SelectedBudget
        {
            get => _selectedBudget;
            set => SetProperty(ref _selectedBudget, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetProperty(ref _isAdmin, value);
        }

        public FamilyMember CurrentFamilyMember
        {
            get => _currentFamilyMember;
            set => SetProperty(ref _currentFamilyMember, value);
        }

        public ObservableCollection<BudgetDto> Budgets { get; } = new();
        public ObservableCollection<BudgetAlertDto> BudgetAlerts { get; } = new();

        public ICommand LoadBudgetsCommand { get; }
        public ICommand EditBudgetCommand { get; }
        public ICommand DeleteBudgetCommand { get; }
        public ICommand RefreshCommand { get; }

        public Action CloseAction { get; set; }

        public BudgetManagementViewModel(FinanceService financeService, Guid currentFamilyId, Guid currentUserId)
        {
            _financeService = financeService;
            _currentFamilyId = currentFamilyId;
            _currentUserId = currentUserId;

            LoadBudgetsCommand = new RelayCommand(async _ => await LoadBudgetsAsync());
            EditBudgetCommand = new RelayCommand(EditBudget, _ => CanEditBudget()); // Изменено: убрана лямбда
            DeleteBudgetCommand = new RelayCommand(DeleteBudget, _ => CanDeleteBudget()); // Изменено: убрана лямбда
            RefreshCommand = new RelayCommand(async _ => await LoadBudgetsAsync());

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadCurrentUserInfoAsync();
                await LoadBudgetsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка инициализации: {ex.Message}";
            }
        }

        private async Task LoadCurrentUserInfoAsync()
        {
            try
            {
                var familyMembers = await _financeService.GetFamilyMembersByFamilyIdAsync(_currentFamilyId);
                CurrentFamilyMember = familyMembers.FirstOrDefault(fm => fm.UserId == _currentUserId);

                if (CurrentFamilyMember != null)
                {
                    IsAdmin = CurrentFamilyMember.Role == "Администратор";
                }
                else
                {
                    IsAdmin = false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки информации о пользователе: {ex.Message}";
                IsAdmin = false;
            }
        }

        private async Task LoadBudgetsAsync()
        {
            try
            {
                ErrorMessage = string.Empty;

                var budgets = await _financeService.GetBudgetsWithDetailsAsync(_currentFamilyId);
                Budgets.Clear();
                foreach (var budget in budgets)
                {
                    Budgets.Add(budget);
                }

                var alerts = await _financeService.GetBudgetAlertsAsync(_currentFamilyId);
                BudgetAlerts.Clear();
                foreach (var alert in alerts)
                {
                    BudgetAlerts.Add(alert);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки бюджетов: {ex.Message}";
            }
        }

        private bool CanEditBudget(object parameter = null) // Добавлен необязательный параметр
        {
            return SelectedBudget != null && IsAdmin;
        }

        private void EditBudget(object parameter) // Теперь принимает параметр
        {
            if (!IsAdmin)
            {
                MessageBox.Show("Только администратор может редактировать бюджеты", "Ошибка прав",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedBudget == null) return;

            try
            {
                MessageBox.Show(
                    $"Редактирование бюджета: {SelectedBudget.CategoryName}\n" +
                    $"Сумма: {SelectedBudget.Amount:C}\n" +
                    $"Период: {SelectedBudget.StartDate:dd.MM.yyyy} - {SelectedBudget.EndDate:dd.MM.yyyy}\n" +
                    $"Потрачено: {SelectedBudget.CurrentSpent:C} ({SelectedBudget.UsagePercentage:F1}%)",
                    "Редактирование бюджета",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при редактировании бюджета: {ex.Message}";
            }
        }

        private bool CanDeleteBudget(object parameter = null) // Добавлен необязательный параметр
        {
            return SelectedBudget != null && IsAdmin;
        }

        private async void DeleteBudget(object parameter) // Принимает параметр
        {
            if (!IsAdmin)
            {
                MessageBox.Show("Только администратор может удалять бюджеты", "Ошибка прав",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedBudget == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить бюджет для категории '{SelectedBudget.CategoryName}'?\n" +
                $"Сумма: {SelectedBudget.Amount:C}\n" +
                $"Период: {SelectedBudget.StartDate:dd.MM.yyyy} - {SelectedBudget.EndDate:dd.MM.yyyy}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var deleteResult = await _financeService.DeleteBudgetAsync(SelectedBudget.Id, _currentUserId);

                    if (deleteResult.IsSuccess)
                    {
                        MessageBox.Show(deleteResult.Message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadBudgetsAsync();
                    }
                    else
                    {
                        ErrorMessage = deleteResult.Message;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка при удалении бюджета: {ex.Message}";
                }
            }
        }
    }
}