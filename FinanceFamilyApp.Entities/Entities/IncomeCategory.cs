using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinanceFamilyApp.Entities
{
    public class IncomeCategory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        // Навигационные свойства
        public ICollection<Income> Incomes { get; set; }
    }
}