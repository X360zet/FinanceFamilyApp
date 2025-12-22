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
    public class ExpenseFormViewModel : ViewModelBase
    {
        private readonly FinanceService _financeService;
        private readonly Guid _currentFamilyId;
        private readonly Guid _currentUserId;
        private decimal _amount;
        private string _description = string.Empty;
        private DateTime _date = DateTime.Today;
        private ExpenseCategory _selectedCategory;
        private FamilyMember _selectedFamilyMember;
        private string _errorMessage = string.Empty;

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public ExpenseCategory SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public FamilyMember SelectedFamilyMember
        {
            get => _selectedFamilyMember;
            set => SetProperty(ref _selectedFamilyMember, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<ExpenseCategory> ExpenseCategories { get; } = new();
        public ObservableCollection<FamilyMember> FamilyMembers { get; } = new();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ExpenseFormViewModel(FinanceService financeService, Guid currentFamilyId, Guid currentUserId)
        {
            _financeService = financeService;
            _currentFamilyId = currentFamilyId;
            _currentUserId = currentUserId;

            SaveCommand = new RelayCommand(SaveExpense, CanSaveExpense);
            CancelCommand = new RelayCommand(Cancel);
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                ErrorMessage = string.Empty;

                var expenseCategories = await _financeService.GetExpenseCategoriesAsync();
                ExpenseCategories.Clear();
                
                foreach (var category in expenseCategories)
                {
                    ExpenseCategories.Add(category);
                }

                var familyMembers = await _financeService.GetFamilyMembersByFamilyIdAsync(_currentFamilyId);
                FamilyMembers.Clear();
                
                foreach (var member in familyMembers)
                {
                    FamilyMembers.Add(member);
                }

                if (ExpenseCategories.Any())
                {
                    SelectedCategory = ExpenseCategories.First();
                }

                if (FamilyMembers.Any())
                {
                    SelectedFamilyMember = FamilyMembers.First();
                }
                else
                {
                    ErrorMessage = "Нет членов семьи. Добавьте члена семьи перед добавлением расхода.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
            }
        }

        private bool CanSaveExpense(object parameter)
        {
            return Amount > 0 && SelectedCategory != null && SelectedFamilyMember != null;
        }

        private async void SaveExpense(object parameter)
        {
            try
            {
                ErrorMessage = string.Empty;

                var expenseDto = new ExpenseDto
                {
                    FamilyMemberId = SelectedFamilyMember.Id,
                    CategoryId = SelectedCategory.Id,
                    Amount = Amount,
                    Description = Description,
                    Date = Date
                };

                var result = await _financeService.AddExpenseAsync(expenseDto, _currentUserId);

                if (result.IsSuccess)
                {
                    MessageBox.Show(result.Message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при добавлении расхода: {ex.Message}";
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