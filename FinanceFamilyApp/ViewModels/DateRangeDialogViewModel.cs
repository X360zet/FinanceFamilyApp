using System;
using System.Windows.Input;
using FinanceFamilyApp.Commands;

namespace FinanceFamilyApp.ViewModels
{
    public class DateRangeDialogViewModel : ViewModelBase
    {
        private DateTime _startDate;
        private DateTime _endDate;

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public Action CloseAction { get; set; }

        public DateRangeDialogViewModel(DateTime defaultStartDate, DateTime defaultEndDate)
        {
            StartDate = defaultStartDate;
            EndDate = defaultEndDate;

            OkCommand = new RelayCommand(Ok);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Ok(object parameter)
        {
            CloseAction?.Invoke();
        }

        private void Cancel(object parameter)
        {
            // Закрываем окно с DialogResult = false (обрабатывается в коде окна)
            // Для этого в коде окна установлено IsCancel=True у кнопки "Отмена"
        }
    }
}