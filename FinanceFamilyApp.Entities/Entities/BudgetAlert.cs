using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceFamilyApp.Entities
{
    public class BudgetAlert
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Budget")]
        public Guid BudgetId { get; set; }

        public Budget? Budget { get; set; }  // Делаем nullable

        [ForeignKey("Expense")]
        public Guid? ExpenseId { get; set; }

        public Expense? Expense { get; set; }  // Делаем nullable

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime AlertDate { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}