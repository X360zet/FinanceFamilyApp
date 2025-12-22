using System;

namespace FinanceFamilyApp.BLL.DTO
{
    public class TransactionDto
    {
        public Guid Id { get; set; } // Добавлено свойство Id
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Username { get; set; }
    }
}