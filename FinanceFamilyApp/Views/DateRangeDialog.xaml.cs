using System.Windows;
using FinanceFamilyApp.ViewModels;

namespace FinanceFamilyApp.Views
{
    public partial class DateRangeDialog : Window
    {
        public DateRangeDialog(DateRangeDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseAction = () => DialogResult = true;
        }

        public DateRangeDialogViewModel ViewModel => (DateRangeDialogViewModel)DataContext;
    }
}