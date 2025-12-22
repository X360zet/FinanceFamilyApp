using FinanceFamilyApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FinanceFamilyApp.Views
{
    public partial class FamilyMemberWindow : Window
    {
        private readonly FamilyMemberViewModel _viewModel;

        public FamilyMemberWindow(FamilyMemberViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Привязка паролей только если не режим редактирования
            if (!_viewModel.IsEditMode)
            {
                PasswordBox.PasswordChanged += (s, e) =>
                {
                    _viewModel.Password = PasswordBox.Password;
                    CommandManager.InvalidateRequerySuggested();
                };

                ConfirmPasswordBox.PasswordChanged += (s, e) =>
                {
                    _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
                    CommandManager.InvalidateRequerySuggested();
                };
            }
            else
            {
                // Скрываем поля паролей в режиме редактирования
                PasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
            }

            _viewModel.CloseAction = () => Close();
        }
    }
}