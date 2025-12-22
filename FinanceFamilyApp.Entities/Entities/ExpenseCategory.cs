using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinanceFamilyApp.Entities
{
    public class ExpenseCategory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } // 'Product', 'Service'

        [MaxLength(500)]
        public string Description { get; set; }

        // Навигационные свойства
        public ICollection<Expense> Expenses { get; set; }
        public ICollection<Budget> Budgets { get; set; }
    }
}