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
    public class IncomeFormViewModel : ViewModelBase
    {
        private readonly FinanceService _financeService;
        private readonly Guid _currentFamilyId;
        private readonly Guid _currentUserId;
        private decimal _amount;
        private string _description = string.Empty;
        private DateTime _date = DateTime.Today;
        private IncomeCategory _selectedCategory;
        private FamilyMember _selectedFamilyMember;

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

        public IncomeCategory SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public FamilyMember SelectedFamilyMember
        {
            get => _selectedFamilyMember;
            set => SetProperty(ref _selectedFamilyMember, value);
        }

        public ObservableCollection<IncomeCategory> IncomeCategories { get; } = new();
        public ObservableCollection<FamilyMember> FamilyMembers { get; } = new();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public IncomeFormViewModel(FinanceService financeService, Guid currentFamilyId, Guid currentUserId)
        {
            _financeService = financeService;
            _currentFamilyId = currentFamilyId;
            _currentUserId = currentUserId;

            SaveCommand = new RelayCommand(SaveIncome, CanSaveIncome);
            CancelCommand = new RelayCommand(Cancel);
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                var incomeCategories = await _financeService.GetIncomeCategoriesAsync();
                IncomeCategories.Clear();
                
                foreach (var category in incomeCategories)
                {
                    IncomeCategories.Add(category);
                }

                var familyMembers = await _financeService.GetFamilyMembersByFamilyIdAsync(_currentFamilyId);
                FamilyMembers.Clear();
                
                foreach (var member in familyMembers)
                {
                    FamilyMembers.Add(member);
                }

                if (IncomeCategories.Any())
                {
                    SelectedCategory = IncomeCategories.First();
                }

                if (FamilyMembers.Any())
                {
                    SelectedFamilyMember = FamilyMembers.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveIncome(object parameter)
        {
            return Amount > 0 && SelectedCategory != null && SelectedFamilyMember != null;
        }

        private async void SaveIncome(object parameter)
        {
            try
            {
                var incomeDto = new IncomeDto
                {
                    FamilyMemberId = SelectedFamilyMember.Id,
                    CategoryId = SelectedCategory.Id,
                    Amount = Amount,
                    Source = "",
                    Description = Description,
                    Date = Date
                };

                var result = await _financeService.AddIncomeAsync(incomeDto, _currentUserId);

                if (result.IsSuccess)
                {
                    MessageBox.Show("Доход успешно добавлен", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    MessageBox.Show($"Ошибка: {result.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении дохода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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