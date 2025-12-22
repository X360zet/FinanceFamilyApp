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
    public class EditTransactionViewModel : ViewModelBase
    {
        private readonly FinanceService _financeService;
        private readonly Guid _currentFamilyId;
        private readonly Guid _currentUserId;
        private readonly ReportItemDto _transaction;
        private decimal _amount;
        private string _description = string.Empty;
        private string _source = string.Empty;
        private DateTime _date;
        private ExpenseCategory _selectedExpenseCategory;
        private IncomeCategory _selectedIncomeCategory;
        private FamilyMember _selectedFamilyMember;
        private string _errorMessage = string.Empty;
        private string _transactionType;

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

        public string Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public ExpenseCategory SelectedExpenseCategory
        {
            get => _selectedExpenseCategory;
            set => SetProperty(ref _selectedExpenseCategory, value);
        }

        public IncomeCategory SelectedIncomeCategory
        {
            get => _selectedIncomeCategory;
            set => SetProperty(ref _selectedIncomeCategory, value);
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

        public string TransactionType
        {
            get => _transactionType;
            private set => SetProperty(ref _transactionType, value);
        }

        public ObservableCollection<ExpenseCategory> ExpenseCategories { get; } = new();
        public ObservableCollection<IncomeCategory> IncomeCategories { get; } = new();
        public ObservableCollection<FamilyMember> FamilyMembers { get; } = new();

        public string Title => $"Редактирование {_transaction.OperationType.ToLower()}а";

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        public EditTransactionViewModel(FinanceService financeService, Guid currentFamilyId, Guid currentUserId, ReportItemDto transaction)
        {
            _financeService = financeService;
            _currentFamilyId = currentFamilyId;
            _currentUserId = currentUserId;
            _transaction = transaction;
            TransactionType = transaction.OperationType;

            SaveCommand = new RelayCommand(SaveTransaction, CanSaveTransaction);
            DeleteCommand = new RelayCommand(DeleteTransaction);
            CancelCommand = new RelayCommand(Cancel);

            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                ErrorMessage = string.Empty;

                if (TransactionType == "Доход")
                {
                    var incomeCategories = await _financeService.GetIncomeCategoriesAsync();
                    IncomeCategories.Clear();
                    
                    foreach (var category in incomeCategories)
                    {
                        IncomeCategories.Add(category);
                    }
                }
                else
                {
                    var expenseCategories = await _financeService.GetExpenseCategoriesAsync();
                    ExpenseCategories.Clear();
                    
                    foreach (var category in expenseCategories)
                    {
                        ExpenseCategories.Add(category);
                    }
                }

                var familyMembers = await _financeService.GetFamilyMembersByFamilyIdAsync(_currentFamilyId);
                FamilyMembers.Clear();
                
                foreach (var member in familyMembers)
                {
                    FamilyMembers.Add(member);
                }

                Amount = _transaction.Amount;
                Description = _transaction.Description;
                Date = _transaction.Date;
                Source = _transaction.Source;

                if (TransactionType == "Доход")
                {
                    var category = IncomeCategories.FirstOrDefault(c => c.Name == _transaction.Category);
                    SelectedIncomeCategory = category ?? IncomeCategories.FirstOrDefault();
                }
                else
                {
                    var category = ExpenseCategories.FirstOrDefault(c => c.Name == _transaction.Category);
                    SelectedExpenseCategory = category ?? ExpenseCategories.FirstOrDefault();
                }

                var familyMember = FamilyMembers.FirstOrDefault(fm => fm.User.Username == _transaction.Username);
                SelectedFamilyMember = familyMember ?? FamilyMembers.FirstOrDefault();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
            }
        }

        private bool CanSaveTransaction(object parameter)
        {
            if (TransactionType == "Доход")
            {
                return Amount > 0 && !string.IsNullOrWhiteSpace(Source) && 
                       SelectedIncomeCategory != null && SelectedFamilyMember != null;
            }
            else
            {
                return Amount > 0 && SelectedExpenseCategory != null && SelectedFamilyMember != null;
            }
        }

        private async void SaveTransaction(object parameter)
        {
            try
            {
                ErrorMessage = string.Empty;
                OperationResult result;

                if (TransactionType == "Доход")
                {
                    var incomeDto = new IncomeDto
                    {
                        FamilyMemberId = SelectedFamilyMember.Id,
                        CategoryId = SelectedIncomeCategory.Id,
                        Amount = Amount,
                        Source = Source,
                        Description = Description,
                        Date = Date
                    };

                    result = await _financeService.UpdateIncomeAsync(incomeDto, _transaction.Id, _currentUserId);
                }
                else
                {
                    var expenseDto = new ExpenseDto
                    {
                        FamilyMemberId = SelectedFamilyMember.Id,
                        CategoryId = SelectedExpenseCategory.Id,
                        Amount = Amount,
                        Description = Description,
                        Date = Date
                    };

                    result = await _financeService.UpdateExpenseAsync(expenseDto, _transaction.Id, _currentUserId);
                }

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
                ErrorMessage = $"Ошибка при обновлении транзакции: {ex.Message}";
            }
        }

        private async void DeleteTransaction(object parameter)
        {
            var result = MessageBox.Show($"Вы уверены, что хотите удалить этот {TransactionType.ToLower()}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    OperationResult deleteResult;

                    if (TransactionType == "Доход")
                    {
                        deleteResult = await _financeService.DeleteIncomeAsync(_transaction.Id, _currentUserId);
                    }
                    else
                    {
                        deleteResult = await _financeService.DeleteExpenseAsync(_transaction.Id, _currentUserId);
                    }

                    if (deleteResult.IsSuccess)
                    {
                        MessageBox.Show(deleteResult.Message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        CloseWindow();
                    }
                    else
                    {
                        ErrorMessage = deleteResult.Message;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка при удалении: {ex.Message}";
                }
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