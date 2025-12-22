using System.Windows;
using FinanceFamilyApp.ViewModels;

namespace FinanceFamilyApp.Views
{
    public partial class CategoryWindow : Window
    {
        public CategoryWindow(CategoryViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseAction = () =>
            {
                DialogResult = true;
                Close();
            };
        }
    }
}