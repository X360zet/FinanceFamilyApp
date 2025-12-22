using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FinanceFamilyApp.BLL.DTO;
using FinanceFamilyApp.BLL.Services;
using FinanceFamilyApp.Commands;
using FinanceFamilyApp.Entities;
using FinanceFamilyApp.Views;

namespace FinanceFamilyApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly FinanceService _financeService;
        private readonly ReportService _reportService;
        private readonly AuthService _authService;
        private decimal _totalIncome;
        private decimal _totalExpense;
        private decimal _balance;
        private DateTime _reportStartDate;
        private DateTime _reportEndDate;
        private User _currentUser;
        private Family _currentFamily;
        private FamilyMember _currentFamilyMember;
        private string _welcomeMessage;
        private ObservableCollection<TransactionDto> _recentTransactions = new();
        private ObservableCollection<ReportItemDto> _reportData = new();
        private ObservableCollection<ReportItemDto> _filteredReportData = new();
        private ObservableCollection<FamilyMember> _familyMembers = new();
        private ObservableCollection<string> _operationTypeFilters = new();
        private ObservableCollection<string> _categoryFilters = new();
        private string _selectedOperationTypeFilter = "Все";
        private string _selectedCategoryFilter = "Все";

        public decimal TotalIncome
        {
            get => _totalIncome;
            set => SetProperty(ref _totalIncome, value);
        }

        public bool IsAdmin => CurrentFamilyMember?.Role == "Администратор";
        public Visibility AdminVisibility => IsAdmin ? Visibility.Visible : Visibility.Collapsed;

        public decimal TotalExpense
        {
            get => _totalExpense;
            set => SetProperty(ref _totalExpense, value);
        }

        public decimal Balance
        {
            get => _balance;
            set
            {
                SetProperty(ref _balance, value);
                OnPropertyChanged(nameof(BalanceColor));
            }
        }

        public string BalanceColor => Balance >= 0 ? "#4CAF50" : "#f44336";

        public DateTime ReportStartDate
        {
            get => _reportStartDate;
            set => SetProperty(ref _reportStartDate, value);
        }

        public DateTime ReportEndDate
        {
            get => _reportEndDate;
            set => SetProperty(ref _reportEndDate, value);
        }

        public User CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public Family CurrentFamily
        {
            get => _currentFamily;
            set => SetProperty(ref _currentFamily, value);
        }

        public FamilyMember CurrentFamilyMember
        {
            get => _currentFamilyMember;
            set => SetProperty(ref _currentFamilyMember, value);
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public ObservableCollection<TransactionDto> RecentTransactions
        {
            get => _recentTransactions;
            set => SetProperty(ref _recentTransactions, value);
        }

        public ObservableCollection<ReportItemDto> ReportData
        {
            get => _reportData;
            set => SetProperty(ref _reportData, value);
        }

        public ObservableCollection<ReportItemDto> FilteredReportData
        {
            get => _filteredReportData;
            set => SetProperty(ref _filteredReportData, value);
        }

        public ObservableCollection<FamilyMember> FamilyMembers
        {
            get => _familyMembers;
            set => SetProperty(ref _familyMembers, value);
        }

        public ObservableCollection<string> OperationTypeFilters
        {
            get => _operationTypeFilters;
            set => SetProperty(ref _operationTypeFilters, value);
        }

        public ObservableCollection<string> CategoryFilters
        {
            get => _categoryFilters;
            set => SetProperty(ref _categoryFilters, value);
        }

        public string SelectedOperationTypeFilter
        {
            get => _selectedOperationTypeFilter;
            set
            {
                SetProperty(ref _selectedOperationTypeFilter, value);
                ApplyFilters();
            }
        }

        public string SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                SetProperty(ref _selectedCategoryFilter, value);
                ApplyFilters();
            }
        }

        public ICommand AddIncomeCommand { get; }
        public ICommand AddExpenseCommand { get; }
        public ICommand SetBudgetCommand { get; }
        public ICommand GenerateReportCommand { get; }
        public ICommand ExportToPdfCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AddFamilyMemberCommand { get; }
        public ICommand EditTransactionCommand { get; }
        public ICommand CreateIncomeCategoryCommand { get; }
        public ICommand CreateExpenseCategoryCommand { get; }
        public ICommand DeleteAllUsersCommand { get; }
        public ICommand ManageBudgetsCommand { get; }
        public ICommand EditFamilyMemberCommand { get; }

        public MainViewModel(FinanceService financeService, ReportService reportService, AuthService authService)
        {
            _financeService = financeService;
            _reportService = reportService;
            _authService = authService;

            ReportStartDate = DateTime.Today.AddMonths(-1);
            ReportEndDate = DateTime.Today;

            InitializeFilters();

            // Команды с проверками прав
            AddIncomeCommand = new RelayCommand(AddIncome);
            AddExpenseCommand = new RelayCommand(AddExpense);
            SetBudgetCommand = new RelayCommand(SetBudget, CanSetBudget); // Только администратор
            GenerateReportCommand = new RelayCommand(GenerateReport);
            ExportToPdfCommand = new RelayCommand(ExportToPdf);
            LogoutCommand = new RelayCommand(Logout);
            AddFamilyMemberCommand = new RelayCommand(AddFamilyMember, CanAddFamilyMember); // Только администратор
            EditTransactionCommand = new RelayCommand(EditTransaction, CanEditTransaction); // Только администратор
            CreateIncomeCategoryCommand = new RelayCommand(CreateIncomeCategory, CanCreateCategory); // Только администратор
            CreateExpenseCategoryCommand = new RelayCommand(CreateExpenseCategory, CanCreateCategory); // Только администратор
            ManageBudgetsCommand = new RelayCommand(ManageBudgets); // Доступно всем
            EditFamilyMemberCommand = new RelayCommand(EditFamilyMember, CanEditFamilyMember); // Только администратор
        }

        private void InitializeFilters()
        {
            OperationTypeFilters = new ObservableCollection<string>
            {
                "Все",
                "Доход",
                "Расход"
            };

            CategoryFilters = new ObservableCollection<string>
            {
                "Все"
            };
        }

        // Проверка прав доступа
        private bool CanSetBudget(object parameter) => IsAdmin;
        private bool CanAddFamilyMember(object parameter) => IsAdmin;
        private bool CanEditTransaction(object parameter) => IsAdmin;
        private bool CanCreateCategory(object parameter) => IsAdmin;
        private bool CanEditFamilyMember(object parameter) => IsAdmin && parameter is FamilyMember;

        public void SetCurrentUser(User user)
        {
            CurrentUser = user;
            WelcomeMessage = $" {user.Username}!";
            _ = SetCurrentUserAsync(user);
        }

        private async Task SetCurrentUserAsync(User user)
        {
            try
            {
                var currentMember = await _financeService.GetFamilyMemberByUserIdAsync(user.Id);

                if (currentMember != null)
                {
                    CurrentFamilyMember = currentMember;
                    var families = await _financeService.GetFamiliesAsync();
                    CurrentFamily = families.FirstOrDefault(f => f.Id == currentMember.FamilyId);

                    if (CurrentFamily != null)
                    {
                        await LoadDashboardDataAsync();
                        await LoadFamilyMembersAsync();
                        await UpdateCategoryFiltersAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadFamilyMembersAsync()
        {
            try
            {
                if (CurrentFamily == null) return;

                FamilyMembers.Clear();
                var members = await _financeService.GetFamilyMembersByFamilyIdAsync(CurrentFamily.Id);

                foreach (var member in members)
                {
                    var user = await _financeService.GetUserByIdAsync(member.UserId);
                    if (user != null)
                    {
                        member.User = user;
                        FamilyMembers.Add(member);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки членов семьи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateCategoryFiltersAsync()
        {
            try
            {
                var categories = new List<string> { "Все" };
                var incomeCategories = await _financeService.GetIncomeCategoriesAsync();
                var expenseCategories = await _financeService.GetExpenseCategoriesAsync();

                foreach (var category in incomeCategories)
                    categories.Add(category.Name);

                foreach (var category in expenseCategories)
                    if (!categories.Contains(category.Name))
                        categories.Add(category.Name);

                CategoryFilters = new ObservableCollection<string>(categories.OrderBy(c => c));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadDashboardDataAsync()
        {
            try
            {
                if (CurrentFamily == null) return;

                var summary = await _financeService.GetFinancialSummaryAsync(
                    CurrentFamily.Id,
                    DateTime.Today.AddMonths(-1),
                    DateTime.Today);

                TotalIncome = summary.TotalIncome;
                TotalExpense = summary.TotalExpense;
                Balance = summary.Balance;

                await LoadRecentTransactionsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadRecentTransactionsAsync()
        {
            if (CurrentFamily == null) return;

            RecentTransactions.Clear();
            var reportData = await _financeService.GetFinancialReportAsync(
                CurrentFamily.Id,
                DateTime.Today.AddDays(-30),
                DateTime.Today);

            foreach (var item in reportData.Take(10))
            {
                RecentTransactions.Add(new TransactionDto
                {
                    Id = item.Id,
                    Date = item.Date,
                    Type = item.OperationType ?? "Неизвестно",
                    Amount = item.Amount,
                    Category = item.Category ?? "Без категории",
                    Description = item.Description ?? "",
                    Username = item.Username ?? "Неизвестно"
                });
            }
        }

        private void AddIncome(object parameter)
        {
            if (CurrentFamily == null)
            {
                MessageBox.Show("Сначала войдите в систему", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var incomeViewModel = new IncomeFormViewModel(_financeService, CurrentFamily.Id, CurrentUser.Id);
                var incomeWindow = new IncomeWindow(incomeViewModel);
                incomeWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                incomeWindow.ShowDialog();

                _ = LoadDashboardDataAsync();
                _ = UpdateCategoryFiltersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddExpense(object parameter)
        {
            if (CurrentFamily == null)
            {
                MessageBox.Show("Сначала войдите в систему", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var expenseViewModel = new ExpenseFormViewModel(_financeService, CurrentFamily.Id, CurrentUser.Id);
                var expenseWindow = new ExpenseWindow(expenseViewModel);
                expenseWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                expenseWindow.ShowDialog();

                _ = LoadDashboardDataAsync();
                _ = UpdateCategoryFiltersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetBudget(object parameter)
        {
            if (CurrentFamily == null)
            {
                MessageBox.Show("Сначала войдите в систему", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsAdmin)
            {
                MessageBox.Show("Только администратор может устанавливать бюджеты", "Ошибка прав",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var budgetViewModel = new BudgetFormViewModel(_financeService, CurrentFamily.Id, CurrentUser.Id);
                var budgetWindow = new BudgetWindow(budgetViewModel);
                budgetWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                budgetWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddFamilyMember(object parameter)
        {
            if (CurrentFamily == null)
            {
                MessageBox.Show("Сначала войдите в систему", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsAdmin)
            {
                MessageBox.Show("Только администратор может добавлять новых членов семьи", "Ошибка прав",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var familyMemberViewModel = new FamilyMemberViewModel(_financeService, _authService, CurrentFamily.Id, CurrentUser.Id);
                var familyMemberWindow = new FamilyMemberWindow(familyMemberViewModel);
                familyMemberWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                familyMemberWindow.ShowDialog();

                _ = LoadFamilyMembersAsync();
                _ = UpdateCategoryFiltersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditFamilyMember(object parameter)
        {
            if (parameter is FamilyMember familyMember)
            {
                if (CurrentFamily == null)
                {
                    MessageBox.Show("Сначала войдите в систему", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!IsAdmin)
                {
                    MessageBox.Show("Только администратор может редактировать членов семьи", "Ошибка прав",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var familyMemberViewModel = new FamilyMemberViewModel(_financeService, _authService,
                        CurrentFamily.Id, CurrentUser.Id, familyMember);

                    var familyMemberWindow = new FamilyMemberWindow(familyMemberViewModel);
                    familyMemberWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    familyMemberWindow.ShowDialog();

                    _ = LoadFamilyMembersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerateReport(object parameter)
        {
            if (CurrentFamily == null)
            {
                MessageBox.Show("Сначала войдите в систему", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _ = GenerateReportAsync();
        }

        private async Task GenerateReportAsync()
        {
            try
            {
                var reportData = await _financeService.GetFinancialReportAsync(
                    CurrentFamily.Id,
                    ReportStartDate,
                    ReportEndDate);

                ReportData = new ObservableCollection<ReportItemDto>(reportData);
                ApplyFilters();

                var summary = await _financeService.GetFinancialSummaryAsync(
                    CurrentFamily.Id,
                    ReportStartDate,
                    ReportEndDate);

                TotalIncome = summary.TotalIncome;
                TotalExpense = summary.TotalExpense;
                Balance = summary.Balance;

                MessageBox.Show($"Отчет сформирован за период с {ReportStartDate:dd.MM.yyyy} по {ReportEndDate:dd.MM.yyyy}",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (ReportData == null || !ReportData.Any())
            {
                FilteredReportData.Clear();
                return;
            }

            var filtered = ReportData.AsEnumerable();

            if (!string.IsNullOrEmpty(SelectedOperationTypeFilter) && SelectedOperationTypeFilter != "Все")
            {
                filtered = filtered.Where(r => r.OperationType == SelectedOperationTypeFilter);
            }

            if (!string.IsNullOrEmpty(SelectedCategoryFilter) && SelectedCategoryFilter != "Все")
            {
                filtered = filtered.Where(r => r.Category == SelectedCategoryFilter);
            }

            FilteredReportData = new ObservableCollection<ReportItemDto>(filtered);
        }

        private void EditTransaction(object parameter)
        {
            if (parameter is TransactionDto transactionDto && CurrentFamily != null)
            {
                if (!IsAdmin)
                {
                    MessageBox.Show("Только администратор может редактировать транзакции", "Ошибка прав",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var reportItem = new ReportItemDto
                    {
                        Id = transactionDto.Id,
                        Date = transactionDto.Date,
                        OperationType = transactionDto.Type,
                        Category = transactionDto.Category,
                        Amount = transactionDto.Amount,
                        Description = transactionDto.Description,
                        Username = transactionDto.Username,
                        Source = "",
                        FamilyMemberRole = ""
                    };

                    var editViewModel = new EditTransactionViewModel(_financeService, CurrentFamily.Id, CurrentUser.Id, reportItem);
                    var editWindow = new EditTransactionWindow(editViewModel);
                    editWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    editWindow.ShowDialog();

                    _ = LoadDashboardDataAsync();
                    _ = GenerateReportAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия формы редактирования: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportToPdf(object parameter)
        {
            _ = ExportToPdfAsync();
        }

        private async Task ExportToPdfAsync()
        {
            if (!FilteredReportData.Any())
            {
                MessageBox.Show("Нет данных для экспорта", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF файлы (*.pdf)|*.pdf",
                FileName = $"Финансовый отчет {DateTime.Now:dd.MM.yyyy}.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    await _reportService.ExportToPdfAsync(FilteredReportData.ToList(), saveDialog.FileName);
                    MessageBox.Show("Отчет экспортирован в PDF", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateIncomeCategory(object parameter)
        {
            if (CurrentFamilyMember?.Role != "Администратор")
            {
                MessageBox.Show("Только администратор может создавать категории", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var categoryViewModel = new CategoryViewModel(_financeService, "Income", CurrentUser.Id);
                var categoryWindow = new CategoryWindow(categoryViewModel);
                categoryWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                if (categoryWindow.ShowDialog() == true)
                {
                    _ = UpdateCategoryFiltersAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateExpenseCategory(object parameter)
        {
            if (CurrentFamilyMember?.Role != "Администратор")
            {
                MessageBox.Show("Только администратор может создавать категории", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var categoryViewModel = new CategoryViewModel(_financeService, "Expense", CurrentUser.Id);
                var categoryWindow = new CategoryWindow(categoryViewModel);
                categoryWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                if (categoryWindow.ShowDialog() == true)
                {
                    _ = UpdateCategoryFiltersAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDeleteAllUsers(object parameter)
        {
            return CurrentFamilyMember?.Role == "Администратор";
        }

        private void Logout(object parameter)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Сбрасываем данные
                    CurrentUser = null;
                    CurrentFamily = null;
                    CurrentFamilyMember = null;
                    WelcomeMessage = string.Empty;
                    TotalIncome = 0;
                    TotalExpense = 0;
                    Balance = 0;
                    RecentTransactions.Clear();
                    ReportData.Clear();
                    FilteredReportData.Clear();
                    FamilyMembers.Clear();

                    // Сбрасываем фильтры
                    InitializeFilters();

                    // Закрываем все окна
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is LoginWindow)
                            continue;
                        window.Close();
                    }

                    // Открываем окно входа
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка выхода: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ManageBudgets(object parameter)
        {
            if (CurrentFamily == null)
            {
                MessageBox.Show("Сначала войдите в систему", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var budgetManagementViewModel = new BudgetManagementViewModel(_financeService, CurrentFamily.Id, CurrentUser.Id);
                var budgetManagementWindow = new BudgetManagementWindow(budgetManagementViewModel);
                budgetManagementWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                budgetManagementWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}