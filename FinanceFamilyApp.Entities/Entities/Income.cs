using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceFamilyApp.Entities
{
    public class Income
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("FamilyMember")]
        public Guid FamilyMemberId { get; set; }
        public FamilyMember FamilyMember { get; set; }

        [ForeignKey("IncomeCategory")]
        public Guid CategoryId { get; set; }
        public IncomeCategory Category { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(200)]
        public string Source { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}