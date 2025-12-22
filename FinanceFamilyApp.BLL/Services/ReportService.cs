using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceFamilyApp.BLL.DTO;

namespace FinanceFamilyApp.BLL.Services
{
    public class ReportService
    {
        public async Task ExportToPdfAsync(List<ReportItemDto> reportData, string filePath)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                // Группируем данные по месяцам для диаграмм
                var monthlyIncomeData = GetMonthlyIncomeData(reportData);
                var monthlyExpenseData = GetMonthlyExpenseData(reportData);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header()
                            .AlignCenter()
                            .Text("Финансовый отчет")
                            .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                // Сводная информация
                                column.Item().PaddingBottom(15)
                                    .Text($"Период отчета: {DateTime.Now:dd.MM.yyyy}")
                                    .FontSize(12);

                                // Итоги
                                column.Item().PaddingBottom(20).Row(row =>
                                {
                                    row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10)
                                        .Text($"Общий доход: {CalculateTotalIncome(reportData):C}")
                                        .Bold();
                                    row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10)
                                        .Text($"Общий расход: {CalculateTotalExpense(reportData):C}")
                                        .Bold();
                                    row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10)
                                        .Text($"Итоговое сальдо: {CalculateBalance(reportData):C}")
                                        .Bold();
                                });

                                // Основная таблица
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(0.8f);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                                            .Text("Дата").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                                            .Text("Тип").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                                            .Text("Сумма").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                                            .Text("Категория").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                                            .Text("Описание").Bold();
                                    });

                                    foreach (var item in reportData.Take(50))
                                    {
                                        var backgroundColor = item.OperationType == "Доход"
                                            ? Colors.Green.Lighten4
                                            : Colors.Red.Lighten4;

                                        table.Cell().Background(backgroundColor).Padding(5)
                                            .Text(item.Date.ToString("dd.MM.yyyy"));
                                        table.Cell().Background(backgroundColor).Padding(5)
                                            .Text(item.OperationType);
                                        table.Cell().Background(backgroundColor).Padding(5)
                                            .Text(item.Amount.ToString("C"));
                                        table.Cell().Background(backgroundColor).Padding(5)
                                            .Text(item.Category);
                                        table.Cell().Background(backgroundColor).Padding(5)
                                            .Text(item.Description);
                                    }
                                });

                                // Если есть данные для диаграмм, добавляем новую страницу
                                if (monthlyIncomeData.Any() || monthlyExpenseData.Any())
                                {
                                    column.Item().PageBreak();

                                    // Заголовок для диаграмм
                                    column.Item().PaddingTop(10)
                                        .Text("Диаграммы доходов и расходов по месяцам")
                                        .SemiBold().FontSize(16)
                                        .AlignCenter();

                                    // Диаграмма доходов по месяцам
                                    if (monthlyIncomeData.Any())
                                    {
                                        column.Item().PaddingTop(20)
                                            .Text("Доходы по месяцам")
                                            .SemiBold().FontSize(14);

                                        column.Item().PaddingTop(10)
                                            .Table(CreateMonthlyChartTable(monthlyIncomeData, Colors.Green.Lighten2));
                                    }

                                    // Диаграмма расходов по месяцам
                                    if (monthlyExpenseData.Any())
                                    {
                                        column.Item().PaddingTop(30)
                                            .Text("Расходы по месяцам")
                                            .SemiBold().FontSize(14);

                                        column.Item().PaddingTop(10)
                                            .Table(CreateMonthlyChartTable(monthlyExpenseData, Colors.Red.Lighten2));
                                    }

                                    // Сравнительная таблица по месяцам
                                    column.Item().PaddingTop(30)
                                        .Text("Сравнительная таблица по месяцам")
                                        .SemiBold().FontSize(14);

                                    column.Item().PaddingTop(10)
                                        .Table(CreateComparisonTable(monthlyIncomeData, monthlyExpenseData));
                                }

                                // Графики распределения по категориям
                                column.Item().PageBreak();
                                column.Item().PaddingTop(20)
                                    .Text("Расходы по категориям")
                                    .SemiBold().FontSize(16);

                                var expenseData = reportData
                                    .Where(x => x.OperationType == "Расход" && x.Amount > 0)
                                    .GroupBy(x => x.Category)
                                    .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
                                    .OrderByDescending(x => x.Amount)
                                    .Take(10)
                                    .ToList();

                                if (expenseData.Any())
                                {
                                    foreach (var item in expenseData)
                                    {
                                        column.Item().PaddingLeft(10)
                                            .Text($"{item.Category}: {item.Amount:C}")
                                            .FontSize(12);
                                    }
                                }
                                else
                                {
                                    column.Item().PaddingLeft(10)
                                        .Text("Нет данных о расходах")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Medium);
                                }

                                // Доходы по категориям
                                column.Item().PaddingTop(20)
                                    .Text("Доходы по категориям")
                                    .SemiBold().FontSize(16);

                                var incomeData = reportData
                                    .Where(x => x.OperationType == "Доход" && x.Amount > 0)
                                    .GroupBy(x => x.Category)
                                    .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
                                    .OrderByDescending(x => x.Amount)
                                    .Take(10)
                                    .ToList();

                                if (incomeData.Any())
                                {
                                    foreach (var item in incomeData)
                                    {
                                        column.Item().PaddingLeft(10)
                                            .Text($"{item.Category}: {item.Amount:C}")
                                            .FontSize(12);
                                    }
                                }
                                else
                                {
                                    column.Item().PaddingLeft(10)
                                        .Text("Нет данных о доходах")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Medium);
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Страница ");
                                x.CurrentPageNumber();
                                x.Span(" из ");
                                x.TotalPages();
                                x.Span($" | Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}");
                            });
                    });
                });

                await Task.Run(() => document.GeneratePdf(filePath));
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка генерации PDF: {ex.Message}", ex);
            }
        }

        // Получение данных по доходам по месяцам
        private List<MonthlyData> GetMonthlyIncomeData(List<ReportItemDto> reportData)
        {
            return reportData
                .Where(x => x.OperationType == "Доход")
                .GroupBy(x => new { x.Date.Year, x.Date.Month })
                .Select(g => new MonthlyData
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(x => x.Amount),
                    MonthName = GetMonthName(g.Key.Month)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();
        }

        // Получение данных по расходам по месяцам
        private List<MonthlyData> GetMonthlyExpenseData(List<ReportItemDto> reportData)
        {
            return reportData
                .Where(x => x.OperationType == "Расход")
                .GroupBy(x => new { x.Date.Year, x.Date.Month })
                .Select(g => new MonthlyData
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(x => x.Amount),
                    MonthName = GetMonthName(g.Key.Month)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();
        }

        // Создание таблицы с диаграммой для месячных данных
        private Action<TableDescriptor> CreateMonthlyChartTable(List<MonthlyData> monthlyData, Color barColor)
        {
            return table =>
            {
                var maxAmount = monthlyData.Any() ? monthlyData.Max(x => x.Amount) : 1m;

                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80); // Название месяца
                    columns.RelativeColumn(3);  // Полоска диаграммы
                    columns.ConstantColumn(80); // Сумма
                });

                foreach (var data in monthlyData)
                {
                    // Месяц
                    table.Cell().PaddingVertical(5).Text($"{data.MonthName} {data.Year}");

                    // Диаграмма (полоска)
                    var percentage = (float)(data.Amount / maxAmount) * 100; // Явное приведение к float
                    table.Cell().PaddingVertical(5).AlignMiddle()
                        .Background(barColor)
                        .Width(percentage) // Исправлено: не нужно указывать Unit для процентов
                        .Height(15)
                        .Text(" ");

                    // Сумма
                    table.Cell().PaddingVertical(5).AlignRight().Text(data.Amount.ToString("C"));
                }
            };
        }

        // Создание сравнительной таблицы по месяцам
        private Action<TableDescriptor> CreateComparisonTable(List<MonthlyData> incomeData, List<MonthlyData> expenseData)
        {
            return table =>
            {
                // Объединяем все месяцы из обоих наборов данных
                var allMonths = incomeData.Select(x => new { x.Year, x.Month, x.MonthName })
                    .Union(expenseData.Select(x => new { x.Year, x.Month, x.MonthName }))
                    .Distinct()
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();

                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(100); // Месяц
                    columns.ConstantColumn(100); // Доходы
                    columns.ConstantColumn(100); // Расходы
                    columns.ConstantColumn(100); // Баланс
                });

                // Заголовок таблицы
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Месяц").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Доходы").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Расходы").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Баланс").Bold();
                });

                // Данные
                foreach (var month in allMonths)
                {
                    var income = incomeData.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month)?.Amount ?? 0;
                    var expense = expenseData.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month)?.Amount ?? 0;
                    var balance = income - expense;

                    // Месяц
                    table.Cell().Padding(5).Text($"{month.MonthName} {month.Year}");

                    // Доходы (зеленый)
                    table.Cell().Padding(5)
                        .Text(income.ToString("C"))
                        .FontColor(Colors.Green.Darken3);

                    // Расходы (красный)
                    table.Cell().Padding(5)
                        .Text(expense.ToString("C"))
                        .FontColor(Colors.Red.Darken3);

                    // Баланс (зависит от знака)
                    var balanceColor = balance >= 0 ? Colors.Green.Darken3 : Colors.Red.Darken3;
                    var balanceCell = table.Cell().Padding(5)
                        .Text(balance.ToString("C"))
                        .FontColor(balanceColor);

                    if (balance != 0)
                    {
                        balanceCell.Bold();
                    }
                }

                // Итоговая строка
                var totalIncome = incomeData.Sum(x => x.Amount);
                var totalExpense = expenseData.Sum(x => x.Amount);
                var totalBalance = totalIncome - totalExpense;

                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Итого").Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                    .Text(totalIncome.ToString("C")).Bold().FontColor(Colors.Green.Darken3);
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                    .Text(totalExpense.ToString("C")).Bold().FontColor(Colors.Red.Darken3);

                var totalBalanceColor = totalBalance >= 0 ? Colors.Green.Darken3 : Colors.Red.Darken3;
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                    .Text(totalBalance.ToString("C")).Bold().FontColor(totalBalanceColor);
            };
        }

        // Вспомогательный метод для получения названия месяца
        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "Январь",
                2 => "Февраль",
                3 => "Март",
                4 => "Апрель",
                5 => "Май",
                6 => "Июнь",
                7 => "Июль",
                8 => "Август",
                9 => "Сентябрь",
                10 => "Октябрь",
                11 => "Ноябрь",
                12 => "Декабрь",
                _ => "Неизвестно"
            };
        }

        private decimal CalculateTotalIncome(List<ReportItemDto> reportData)
        {
            return reportData
                .Where(x => x.OperationType == "Доход")
                .Sum(x => x.Amount);
        }

        private decimal CalculateTotalExpense(List<ReportItemDto> reportData)
        {
            return reportData
                .Where(x => x.OperationType == "Расход")
                .Sum(x => x.Amount);
        }

        private decimal CalculateBalance(List<ReportItemDto> reportData)
        {
            return CalculateTotalIncome(reportData) - CalculateTotalExpense(reportData);
        }

        // Вспомогательный класс для месячных данных
        private class MonthlyData
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public string MonthName { get; set; }
            public decimal Amount { get; set; }
        }
    }
}