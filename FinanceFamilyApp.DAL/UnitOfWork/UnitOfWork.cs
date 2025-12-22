using FinanceFamilyApp.DAL.Data;
using FinanceFamilyApp.DAL.Repositories;
using FinanceFamilyApp.Entities;

namespace FinanceFamilyApp.DAL
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new GenericRepository<User>(_context);
            Families = new GenericRepository<Family>(_context);
            FamilyMembers = new GenericRepository<FamilyMember>(_context);
            IncomeCategories = new GenericRepository<IncomeCategory>(_context);
            ExpenseCategories = new GenericRepository<ExpenseCategory>(_context);
            Incomes = new GenericRepository<Income>(_context);
            Expenses = new GenericRepository<Expense>(_context);
            Budgets = new GenericRepository<Budget>(_context);
            BudgetAlerts = new GenericRepository<BudgetAlert>(_context);
        }

        public IRepository<User> Users { get; }
        public IRepository<Family> Families { get; }
        public IRepository<FamilyMember> FamilyMembers { get; }
        public IRepository<IncomeCategory> IncomeCategories { get; }
        public IRepository<ExpenseCategory> ExpenseCategories { get; }
        public IRepository<Income> Incomes { get; }
        public IRepository<Expense> Expenses { get; }
        public IRepository<Budget> Budgets { get; }
        public IRepository<BudgetAlert> BudgetAlerts { get; }

        public async Task<int> SaveChangesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return await _context.SaveChangesAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
            _semaphore?.Dispose();
        }
    }
}