using Microsoft.EntityFrameworkCore;
using FinanceFamilyApp.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Linq;

namespace FinanceFamilyApp.DAL.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Family> Families { get; set; } = null!;
        public DbSet<FamilyMember> FamilyMembers { get; set; } = null!;
        public DbSet<IncomeCategory> IncomeCategories { get; set; } = null!;
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; } = null!;
        public DbSet<Income> Incomes { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<Budget> Budgets { get; set; } = null!;
        public DbSet<BudgetAlert> BudgetAlerts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Отключаем каскадное удаление для всех связей
            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // Настройка уникальных индексов
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            });

            // Настройка точности для денежных полей
            modelBuilder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Income>()
                .Property(i => i.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Budget>()
                .Property(b => b.Amount)
                .HasPrecision(18, 2);

            // Настройка таблиц
            ConfigureFamily(modelBuilder);
            ConfigureFamilyMember(modelBuilder);
            ConfigureIncomeCategory(modelBuilder);
            ConfigureExpenseCategory(modelBuilder);
            ConfigureIncome(modelBuilder);
            ConfigureExpense(modelBuilder);
            ConfigureBudget(modelBuilder);
            ConfigureBudgetAlert(modelBuilder);

            // Добавление начальных данных
            SeedData(modelBuilder);
        }

        private void ConfigureFamily(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Family>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            });
        }

        private void ConfigureFamilyMember(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FamilyMember>(entity =>
            {
                entity.HasOne(fm => fm.Family)
                    .WithMany(f => f.FamilyMembers)
                    .HasForeignKey(fm => fm.FamilyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fm => fm.User)
                    .WithMany()
                    .HasForeignKey(fm => fm.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            });
        }

        private void ConfigureIncomeCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IncomeCategory>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
            });
        }

        private void ConfigureExpenseCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExpenseCategory>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
            });
        }

        private void ConfigureIncome(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Income>(entity =>
            {
                entity.HasOne(i => i.FamilyMember)
                    .WithMany()
                    .HasForeignKey(i => i.FamilyMemberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Category)
                    .WithMany(ic => ic.Incomes)
                    .HasForeignKey(i => i.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Source).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            });
        }

        private void ConfigureExpense(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasOne(e => e.FamilyMember)
                    .WithMany()
                    .HasForeignKey(e => e.FamilyMemberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Category)
                    .WithMany(ec => ec.Expenses)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            });
        }

        private void ConfigureBudget(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Budget>(entity =>
            {
                entity.HasOne(b => b.Family)
                    .WithMany()
                    .HasForeignKey(b => b.FamilyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Category)
                    .WithMany(ec => ec.Budgets)
                    .HasForeignKey(b => b.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.PeriodType).HasMaxLength(20).IsRequired();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            });
        }

        private void ConfigureBudgetAlert(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BudgetAlert>(entity =>
            {
                entity.HasOne(ba => ba.Budget)
                    .WithMany()
                    .HasForeignKey(ba => ba.BudgetId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ba => ba.Expense)
                    .WithMany()
                    .HasForeignKey(ba => ba.ExpenseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.AlertDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsRead).HasDefaultValue(false);
            });
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Создание начальных категорий доходов
            var salaryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var freelanceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var investmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            var incomeCategories = new[]
            {
                new IncomeCategory { Id = salaryId, Name = "Зарплата", Description = "Основная заработная плата" },
                new IncomeCategory { Id = freelanceId, Name = "Подработка", Description = "Дополнительный заработок" },
                new IncomeCategory { Id = investmentId, Name = "Инвестиции", Description = "Доход от инвестиций" },
                new IncomeCategory { Id = Guid.NewGuid(), Name = "Пенсия", Description = "Пенсионные выплаты" },
                new IncomeCategory { Id = Guid.NewGuid(), Name = "Стипендия", Description = "Стипендиальные выплаты" }
            };

            modelBuilder.Entity<IncomeCategory>().HasData(incomeCategories);

            // Создание начальных категорий расходов
            var foodId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var utilitiesId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            var transportId = Guid.Parse("66666666-6666-6666-6666-666666666666");

            var expenseCategories = new[]
            {
                new ExpenseCategory { Id = foodId, Name = "Продукты питания", Type = "Product", Description = "Покупка продуктов" },
                new ExpenseCategory { Id = utilitiesId, Name = "Коммунальные услуги", Type = "Service", Description = "Оплата ЖКХ" },
                new ExpenseCategory { Id = transportId, Name = "Транспорт", Type = "Service", Description = "Транспортные расходы" },
                new ExpenseCategory { Id = Guid.NewGuid(), Name = "Образование", Type = "Service", Description = "Расходы на обучение" },
                new ExpenseCategory { Id = Guid.NewGuid(), Name = "Развлечения", Type = "Service", Description = "Развлекательные мероприятия" },
                new ExpenseCategory { Id = Guid.NewGuid(), Name = "Здоровье", Type = "Service", Description = "Медицинские расходы" },
                new ExpenseCategory { Id = Guid.NewGuid(), Name = "Одежда", Type = "Product", Description = "Покупка одежды" },
                new ExpenseCategory { Id = Guid.NewGuid(), Name = "Техника", Type = "Product", Description = "Покупка электроники" }
            };

            modelBuilder.Entity<ExpenseCategory>().HasData(expenseCategories);

            // Создание тестового пользователя (опционально, если хотите начальные данные)
            var adminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            var users = new[]
            {
                new User
                {
                    Id = adminUserId,
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    CreatedDate = DateTime.UtcNow
                }
            };

            modelBuilder.Entity<User>().HasData(users);
        }
    }
}