using System.Threading.Tasks;
using FinanceFamilyApp.DAL.Repositories;
using FinanceFamilyApp.Entities;

namespace FinanceFamilyApp.DAL
{
    public interface IUnitOfWork
    {
        IRepository<User> Users { get; }
        IRepository<Family> Families { get; }
        IRepository<FamilyMember> FamilyMembers { get; }
        IRepository<IncomeCategory> IncomeCategories { get; }
        IRepository<ExpenseCategory> ExpenseCategories { get; }
        IRepository<Income> Incomes { get; }
        IRepository<Expense> Expenses { get; }
        IRepository<Budget> Budgets { get; }
        IRepository<BudgetAlert> BudgetAlerts { get; }

        Task<int> SaveChangesAsync();
    }
}