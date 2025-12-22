using System.Windows;
using FinanceFamilyApp.ViewModels;

namespace FinanceFamilyApp.Views
{
    public partial class IncomeWindow : Window
    {
        public IncomeWindow(IncomeFormViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}