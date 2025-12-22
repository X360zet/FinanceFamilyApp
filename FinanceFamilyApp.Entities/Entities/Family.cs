using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinanceFamilyApp.Entities
{
    public class Family
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        // Навигационные свойства
        public ICollection<FamilyMember> FamilyMembers { get; set; }
    }
}