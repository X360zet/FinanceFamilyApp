namespace FinanceFamilyApp.BLL.DTO
{
    public class BudgetDto
    {
        public Guid Id { get; set; }
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
        public string PeriodType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal CurrentSpent { get; set; }
        public decimal RemainingAmount => Amount - CurrentSpent;
        public decimal UsagePercentage => Amount > 0 ? (CurrentSpent / Amount) * 100 : 0;
        public bool IsExceeded => CurrentSpent > Amount;
    }
}