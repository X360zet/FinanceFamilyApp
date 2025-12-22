using System;

namespace FinanceFamilyApp.BLL.DTO
{
    public class CategoryReportDto
    {
        public string Category { get; set; }
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageAmount { get; set; }
    }
}