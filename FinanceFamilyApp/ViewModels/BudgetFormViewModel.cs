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
    public class BudgetFormViewModel : ViewModelBase
    {
        private readonly FinanceService _financeService;
        private readonly Guid _currentFamilyId;
        private readonly Guid _currentUserId;
        private decimal _amount;
        private string _periodType = "Месячный";
        private DateTime _startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime _endDate = DateTime.Today.AddMonths(1).AddDays(-1);
        private ExpenseCategory _selectedCategory;
        private Guid? _editingBudgetId;
        private string _errorMessage = string.Empty;

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public string PeriodType
        {
            get => _periodType;
            set => SetProperty(ref _periodType, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public ExpenseCategory SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<ExpenseCategory> ExpenseCategories { get; } = new();
        public ObservableCollection<string> PeriodTypes { get; } = new()
        {
            "Ежедневный",
            "Еженедельный",
            "Месячный",
            "Квартальный",
            "Годовой"
        };

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public BudgetFormViewModel(FinanceService financeService, Guid currentFamilyId, Guid currentUserId, Budget existingBudget = null)
        {
            _financeService = financeService;
            _currentFamilyId = currentFamilyId;
            _currentUserId = currentUserId;

            if (existingBudget != null)
            {
                _editingBudgetId = existingBudget.Id;
                Amount = existingBudget.Amount;
                PeriodType = existingBudget.PeriodType;
                StartDate = existingBudget.StartDate;
                EndDate = existingBudget.EndDate;
            }

            SaveCommand = new RelayCommand(SaveBudget, CanSaveBudget);
            CancelCommand = new RelayCommand(Cancel);
            LoadData();
        }

        public BudgetFormViewModel(FinanceService financeService, Guid currentFamilyId, Guid currentUserId)
        {
            _financeService = financeService;
            _currentFamilyId = currentFamilyId;
            _currentUserId = currentUserId;

            SaveCommand = new RelayCommand(SaveBudget, CanSaveBudget);
            CancelCommand = new RelayCommand(Cancel);
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                var expenseCategories = await _financeService.GetExpenseCategoriesAsync();
                ExpenseCategories.Clear();
                
                foreach (var category in expenseCategories)
                {
                    ExpenseCategories.Add(category);
                }

                if (ExpenseCategories.Any())
                {
                    SelectedCategory = ExpenseCategories.First();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
            }
        }

        private bool CanSaveBudget(object parameter)
        {
            return Amount > 0 && SelectedCategory != null && StartDate < EndDate;
        }

        private async void SaveBudget(object parameter)
        {
            try
            {
                var budget = new Budget
                {
                    Id = _editingBudgetId ?? Guid.NewGuid(),
                    FamilyId = _currentFamilyId,
                    CategoryId = SelectedCategory.Id,
                    Amount = Amount,
                    PeriodType = PeriodType,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    CreatedDate = DateTime.UtcNow
                };

                OperationResult result;
                if (_editingBudgetId.HasValue)
                {
                    result = await _financeService.UpdateBudgetAsync(budget, _currentUserId);
                }
                else
                {
                    result = await _financeService.AddBudgetAsync(budget, _currentUserId);
                }

                if (result.IsSuccess)
                {
                    MessageBox.Show(result.Message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    ErrorMessage = $"Ошибка: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении бюджета: {ex.Message}";
            }
        }

        private void Cancel(object parameter)
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