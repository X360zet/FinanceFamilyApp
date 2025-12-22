using System;

namespace FinanceFamilyApp.BLL.DTO
{
    public class ExpenseDto
    {
        public Guid FamilyMemberId { get; set; }
        public Guid CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
    }
}