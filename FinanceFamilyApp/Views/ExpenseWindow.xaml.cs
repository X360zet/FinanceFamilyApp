using System.Windows;
using FinanceFamilyApp.ViewModels;

namespace FinanceFamilyApp.Views
{
    public partial class ExpenseWindow : Window
    {
        // Конструктор с параметром для DI
        public ExpenseWindow(ExpenseFormViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // Конструктор по умолчанию для дизайнера
        public ExpenseWindow()
        {
            InitializeComponent();
        }
    }
}