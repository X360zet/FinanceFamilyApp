using System;

namespace FinanceFamilyApp.BLL.DTO
{
    public class BudgetAlertDto
    {
        public Guid Id { get; set; }
        public Guid BudgetId { get; set; }
        public string CategoryName { get; set; }
        public string Message { get; set; }
        public DateTime AlertDate { get; set; }
        public decimal BudgetAmount { get; set; }
        public decimal CurrentSpent { get; set; }
        public double Percentage { get; set; } // Изменено на double
        public bool IsCritical { get; set; } // true если превышен бюджет
    }
}