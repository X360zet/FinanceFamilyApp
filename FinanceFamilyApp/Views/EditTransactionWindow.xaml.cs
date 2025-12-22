using System.Windows;
using FinanceFamilyApp.ViewModels;

namespace FinanceFamilyApp.Views
{
    public partial class EditTransactionWindow : Window
    {
        public EditTransactionWindow(EditTransactionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}