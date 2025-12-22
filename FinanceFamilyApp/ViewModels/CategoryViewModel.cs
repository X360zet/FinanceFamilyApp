using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using FinanceFamilyApp.BLL.Services;
using FinanceFamilyApp.Commands;
using FinanceFamilyApp.Entities;

namespace FinanceFamilyApp.ViewModels
{
    public class CategoryViewModel : ViewModelBase
    {
        private readonly FinanceService _financeService;
        private readonly Guid _currentUserId;
        private readonly string _categoryType;
        private string _categoryName;
        private string _description = string.Empty;
        private string _selectedCategoryType = "Service";
        private string _errorMessage = string.Empty;

        public string CategoryName
        {
            get => _categoryName;
            set => SetProperty(ref _categoryName, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string SelectedCategoryType
        {
            get => _selectedCategoryType;
            set => SetProperty(ref _selectedCategoryType, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string Title => _categoryType == "Income" 
            ? "Создание категории доходов" 
            : "Создание категории расходов";

        public ObservableCollection<string> CategoryTypes { get; } = new() { "Product", "Service" };

        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }
        public Action CloseAction { get; set; }

        public CategoryViewModel(FinanceService financeService, string categoryType, Guid currentUserId)
        {
            _financeService = financeService;
            _categoryType = categoryType;
            _currentUserId = currentUserId;

            CreateCommand = new RelayCommand(CreateCategory, CanCreateCategory);
            CancelCommand = new RelayCommand(Cancel);
        }

        private bool CanCreateCategory(object parameter)
        {
            return !string.IsNullOrWhiteSpace(CategoryName) && CategoryName.Length >= 2;
        }

        private async void CreateCategory(object parameter)
        {
            try
            {
                ErrorMessage = string.Empty;

                if (_categoryType == "Income")
                {
                    var category = new IncomeCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = CategoryName.Trim(),
                        Description = Description.Trim()
                    };

                    var result = await _financeService.AddIncomeCategoryAsync(category, _currentUserId);
                    
                    if (result.IsSuccess)
                    {
                        CloseAction?.Invoke();
                    }
                    else
                    {
                        ErrorMessage = result.Message;
                    }
                }
                else
                {
                    var category = new ExpenseCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = CategoryName.Trim(),
                        Type = SelectedCategoryType,
                        Description = Description.Trim()
                    };

                    var result = await _financeService.AddExpenseCategoryAsync(category, _currentUserId);
                    
                    if (result.IsSuccess)
                    {
                        CloseAction?.Invoke();
                    }
                    else
                    {
                        ErrorMessage = result.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при создании категории: {ex.Message}";
            }
        }

        private void Cancel(object parameter)
        {
            CloseAction?.Invoke();
        }
    }
}