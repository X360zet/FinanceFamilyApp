using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FinanceFamilyApp.BLL.DTO;
using LiveCharts;
using LiveCharts.Wpf;

namespace FinanceFamilyApp.ViewModels
{
    public class ChartsViewModel : ViewModelBase
    {
        private DateTime _startDate;
        private DateTime _endDate;
        private SeriesCollection _incomeExpenseSeries;
        private SeriesCollection _categoryExpenseSeries;
        private SeriesCollection _categoryIncomeSeries;
        private SeriesCollection _monthlyComparisonSeries;
        private Func<double, string> _formatter;
        private ObservableCollection<string> _labels;
        private ObservableCollection<string> _monthlyLabels;

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

        public SeriesCollection IncomeExpenseSeries
        {
            get => _incomeExpenseSeries;
            set => SetProperty(ref _incomeExpenseSeries, value);
        }

        public SeriesCollection CategoryExpenseSeries
        {
            get => _categoryExpenseSeries;
            set => SetProperty(ref _categoryExpenseSeries, value);
        }

        public SeriesCollection CategoryIncomeSeries
        {
            get => _categoryIncomeSeries;
            set => SetProperty(ref _categoryIncomeSeries, value);
        }

        public SeriesCollection MonthlyComparisonSeries
        {
            get => _monthlyComparisonSeries;
            set => SetProperty(ref _monthlyComparisonSeries, value);
        }

        public ObservableCollection<string> Labels
        {
            get => _labels;
            set => SetProperty(ref _labels, value);
        }

        public ObservableCollection<string> MonthlyLabels
        {
            get => _monthlyLabels;
            set => SetProperty(ref _monthlyLabels, value);
        }

        public Func<double, string> Formatter
        {
            get => _formatter;
            set => SetProperty(ref _formatter, value);
        }

        public ChartsViewModel()
        {
            InitializeCharts();
        }

        private void InitializeCharts()
        {
            // Инициализация пустых графиков
            IncomeExpenseSeries = new SeriesCollection();
            CategoryExpenseSeries = new SeriesCollection();
            CategoryIncomeSeries = new SeriesCollection();
            MonthlyComparisonSeries = new SeriesCollection();
            Labels = new ObservableCollection<string>();
            MonthlyLabels = new ObservableCollection<string>();

            Formatter = value => value.ToString("C");
        }

        public void UpdateCharts(List<ReportItemDto> reportData, DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;

            if (reportData == null || !reportData.Any())
            {
                ClearCharts();
                return;
            }

            UpdateIncomeExpenseChart(reportData);
            UpdateCategoryCharts(reportData);
            UpdateMonthlyComparisonChart(reportData);
        }

        private void UpdateIncomeExpenseChart(List<ReportItemDto> reportData)
        {
            // Группируем по дням
            var dailyData = reportData
                .GroupBy(r => r.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Income = g.Where(x => x.OperationType == "Доход").Sum(x => x.Amount),
                    Expense = g.Where(x => x.OperationType == "Расход").Sum(x => x.Amount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            Labels.Clear();
            foreach (var day in dailyData)
            {
                Labels.Add(day.Date.ToString("dd.MM"));
            }

            // Создаем серии для доходов
            var incomeSeries = new LineSeries
            {
                Title = "Доходы",
                Values = new ChartValues<decimal>(dailyData.Select(d => d.Income)),
                PointGeometrySize = 10,
                StrokeThickness = 3,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = System.Windows.Media.Brushes.Green
            };

            // Создаем серии для расходов
            var expenseSeries = new LineSeries
            {
                Title = "Расходы",
                Values = new ChartValues<decimal>(dailyData.Select(d => d.Expense)),
                PointGeometrySize = 10,
                StrokeThickness = 3,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = System.Windows.Media.Brushes.Red
            };

            IncomeExpenseSeries = new SeriesCollection { incomeSeries, expenseSeries };
        }

        private void UpdateCategoryCharts(List<ReportItemDto> reportData)
        {
            // Группируем расходы по категориям
            var categoryExpenses = reportData
                .Where(r => r.OperationType == "Расход")
                .GroupBy(r => r.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Amount = g.Sum(r => r.Amount)
                })
                .Where(x => x.Amount > 0)
                .OrderByDescending(x => x.Amount)
                .Take(10) // Берем топ-10 категорий
                .ToList();

            CategoryExpenseSeries.Clear();

            // Цвета для категорий расходов
            var expenseColors = new[]
            {
                System.Windows.Media.Brushes.Red,
                System.Windows.Media.Brushes.OrangeRed,
                System.Windows.Media.Brushes.DarkOrange,
                System.Windows.Media.Brushes.Orange,
                System.Windows.Media.Brushes.Gold,
                System.Windows.Media.Brushes.Yellow,
                System.Windows.Media.Brushes.YellowGreen,
                System.Windows.Media.Brushes.LightGreen,
                System.Windows.Media.Brushes.Green,
                System.Windows.Media.Brushes.DarkGreen
            };

            for (int i = 0; i < categoryExpenses.Count; i++)
            {
                var category = categoryExpenses[i];
                var pieSeries = new PieSeries
                {
                    Title = category.Category,
                    Values = new ChartValues<decimal> { category.Amount },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.SeriesView.Title}: {chartPoint.Y:C}",
                    Fill = expenseColors[i % expenseColors.Length]
                };

                CategoryExpenseSeries.Add(pieSeries);
            }

            // Группируем доходы по категориям
            var categoryIncomes = reportData
                .Where(r => r.OperationType == "Доход")
                .GroupBy(r => r.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Amount = g.Sum(r => r.Amount)
                })
                .Where(x => x.Amount > 0)
                .OrderByDescending(x => x.Amount)
                .Take(10) // Берем топ-10 категорий
                .ToList();

            CategoryIncomeSeries.Clear();

            // Цвета для категорий доходов
            var incomeColors = new[]
            {
                System.Windows.Media.Brushes.LightBlue,
                System.Windows.Media.Brushes.CornflowerBlue,
                System.Windows.Media.Brushes.RoyalBlue,
                System.Windows.Media.Brushes.Blue,
                System.Windows.Media.Brushes.DarkBlue,
                System.Windows.Media.Brushes.Indigo,
                System.Windows.Media.Brushes.Purple,
                System.Windows.Media.Brushes.Violet,
                System.Windows.Media.Brushes.Plum,
                System.Windows.Media.Brushes.Orchid
            };

            for (int i = 0; i < categoryIncomes.Count; i++)
            {
                var category = categoryIncomes[i];
                var pieSeries = new PieSeries
                {
                    Title = category.Category,
                    Values = new ChartValues<decimal> { category.Amount },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.SeriesView.Title}: {chartPoint.Y:C}",
                    Fill = incomeColors[i % incomeColors.Length]
                };

                CategoryIncomeSeries.Add(pieSeries);
            }
        }

        private void UpdateMonthlyComparisonChart(List<ReportItemDto> reportData)
        {
            // Группируем по месяцам
            var monthlyData = reportData
                .GroupBy(r => new { r.Date.Year, r.Date.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Income = g.Where(x => x.OperationType == "Доход").Sum(x => x.Amount),
                    Expense = g.Where(x => x.OperationType == "Расход").Sum(x => x.Amount),
                    Balance = g.Where(x => x.OperationType == "Доход").Sum(x => x.Amount) -
                              g.Where(x => x.OperationType == "Расход").Sum(x => x.Amount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            MonthlyLabels.Clear();
            foreach (var month in monthlyData)
            {
                MonthlyLabels.Add($"{month.Month:00}.{month.Year}");
            }

            // Создаем столбчатые диаграммы
            var incomeColumnSeries = new ColumnSeries
            {
                Title = "Доходы",
                Values = new ChartValues<decimal>(monthlyData.Select(m => m.Income)),
                Fill = System.Windows.Media.Brushes.Green
            };

            var expenseColumnSeries = new ColumnSeries
            {
                Title = "Расходы",
                Values = new ChartValues<decimal>(monthlyData.Select(m => m.Expense)),
                Fill = System.Windows.Media.Brushes.Red
            };

            var balanceLineSeries = new LineSeries
            {
                Title = "Баланс",
                Values = new ChartValues<decimal>(monthlyData.Select(m => m.Balance)),
                Stroke = System.Windows.Media.Brushes.Blue,
                StrokeThickness = 3,
                PointGeometrySize = 10
            };

            MonthlyComparisonSeries = new SeriesCollection
            {
                incomeColumnSeries,
                expenseColumnSeries,
                balanceLineSeries
            };
        }

        public void ClearCharts()
        {
            IncomeExpenseSeries.Clear();
            CategoryExpenseSeries.Clear();
            CategoryIncomeSeries.Clear();
            MonthlyComparisonSeries.Clear();
            Labels.Clear();
            MonthlyLabels.Clear();
        }

        public decimal GetTotalIncome(List<ReportItemDto> reportData)
        {
            return reportData?
                .Where(x => x.OperationType == "Доход")
                .Sum(x => x.Amount) ?? 0;
        }

        public decimal GetTotalExpense(List<ReportItemDto> reportData)
        {
            return reportData?
                .Where(x => x.OperationType == "Расход")
                .Sum(x => x.Amount) ?? 0;
        }

        public decimal GetBalance(List<ReportItemDto> reportData)
        {
            return GetTotalIncome(reportData) - GetTotalExpense(reportData);
        }
    }
}