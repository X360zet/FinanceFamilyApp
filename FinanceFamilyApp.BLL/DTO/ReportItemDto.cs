using System;

namespace FinanceFamilyApp.BLL.DTO
{
    public class ReportItemDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FamilyMemberRole { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}