using System.Windows;
using FinanceFamilyApp.ViewModels;

namespace FinanceFamilyApp.Views
{
    public partial class BudgetWindow : Window
    {
        // Конструктор с параметром для DI
        public BudgetWindow(BudgetFormViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // Конструктор по умолчанию для дизайнера
        public BudgetWindow()
        {
            InitializeComponent();
        }
    }
}