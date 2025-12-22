using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceFamilyApp.Entities
{
    public class Budget
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Family")]
        public Guid FamilyId { get; set; }
        public Family Family { get; set; }

        [ForeignKey("ExpenseCategory")]
        public Guid CategoryId { get; set; }
        public ExpenseCategory Category { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        public string PeriodType { get; set; } // 'Monthly', 'Weekly', 'Yearly'

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}