using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceFamilyApp.Entities
{
    public class FamilyMember
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Family")]
        public Guid FamilyId { get; set; }
        public Family Family { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } // 'Parent', 'Child', 'Spouse'

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}