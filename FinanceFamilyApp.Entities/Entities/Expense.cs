using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceFamilyApp.Entities
{
    public class Expense
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("FamilyMember")]
        public Guid FamilyMemberId { get; set; }
        public FamilyMember FamilyMember { get; set; }

        [ForeignKey("ExpenseCategory")]
        public Guid CategoryId { get; set; }
        public ExpenseCategory Category { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}