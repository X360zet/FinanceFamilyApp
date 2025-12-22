using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceFamilyApp.DAL;
using FinanceFamilyApp.Entities;
using FinanceFamilyApp.BLL.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FinanceFamilyApp.BLL.Services
{
    public class FinanceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FinanceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        #region Вспомогательные методы для работы с БД

        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    return await operation();
                }
                catch (SqlException sqlEx) when (sqlEx.Number == -2 || sqlEx.Number == 1205) // Timeout or deadlock
                {
                    if (retryCount >= maxRetries)
                        throw;
                    retryCount++;
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount))); // Exponential backoff
                }
                catch (SqlException sqlEx) when (sqlEx.Number == 4060) // Cannot open database
                {
                    throw new Exception($"Не удалось открыть базу данных. Убедитесь, что база данных 'FinanceFamilyDB' существует.");
                }
                catch (SqlException sqlEx) when (sqlEx.Number == 18456) // Login failed
                {
                    throw new Exception($"Ошибка аутентификации при подключении к базе данных.");
                }
                catch (SqlException sqlEx) when (sqlEx.Number == 233) // SQL Server not found
                {
                    throw new Exception($"SQL Server не найден. Убедитесь, что SQL Server запущен и доступен.");
                }
                catch (SqlException sqlEx) when (sqlEx.Number == 53) // Network error
                {
                    throw new Exception($"Сетевая ошибка подключения к SQL Server. Проверьте настройки сети.");
                }
            }
        }

        private async Task ExecuteWithRetryAsync(Func<Task> operation, int maxRetries = 3)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await operation();
                return true;
            }, maxRetries);
        }

        public async Task<bool> TestDatabaseConnectionAsync()
        {
            try
            {
                // Проверяем подключение через контекст
                return await _unitOfWork.Users.ExistsAsync(u => true);
            }
            catch (SqlException sqlEx)
            {
                throw new Exception($"Ошибка подключения к SQL Server: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка подключения к базе данных: {ex.Message}", ex);
            }
        }

        #endregion

        #region Работа с пользователями

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await _unitOfWork.Users.GetAllAsync();
                }
                catch (Exception)
                {
                    return new List<User>();
                }
            });
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await _unitOfWork.Users.GetByIdAsync(userId);
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var users = await _unitOfWork.Users.FindAsync(u => u.Username == username);
                    return users.FirstOrDefault();
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var users = await _unitOfWork.Users.FindAsync(u => u.Username == username);
                    return users.Any();
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        #endregion

        #region Работа с семьями

        public async Task<IEnumerable<Family>> GetFamiliesAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await _unitOfWork.Families.GetAllAsync();
                }
                catch (Exception)
                {
                    return new List<Family>();
                }
            });
        }

        public async Task<Family> GetFamilyByIdAsync(Guid familyId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await _unitOfWork.Families.GetByIdAsync(familyId);
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task<Family> GetFamilyByUserIdAsync(Guid userId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var familyMember = await GetFamilyMemberByUserIdAsync(userId);
                    if (familyMember == null)
                        return null;

                    return await GetFamilyByIdAsync(familyMember.FamilyId);
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task<OperationResult> CreateFamilyAsync(string familyName, Guid creatorUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var user = await GetUserByIdAsync(creatorUserId);
                    if (user == null)
                        return OperationResult.Failure("Пользователь не найден");

                    // Создаем семью
                    var family = new Family
                    {
                        Id = Guid.NewGuid(),
                        Name = familyName,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _unitOfWork.Families.AddAsync(family);

                    // Добавляем создателя как главу семьи
                    var familyMember = new FamilyMember
                    {
                        Id = Guid.NewGuid(),
                        FamilyId = family.Id,
                        UserId = creatorUserId,
                        Role = "Администратор",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _unitOfWork.FamilyMembers.AddAsync(familyMember);

                    return OperationResult.Success($"Семья '{familyName}' успешно создана");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при создании семьи: {ex.Message}");
                }
            });
        }

        #endregion

        #region Работа с членами семьи (с проверкой прав администратора)

        public async Task<IEnumerable<FamilyMember>> GetAllFamilyMembersAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var members = await _unitOfWork.FamilyMembers.GetAllAsync();
                    foreach (var member in members)
                    {
                        member.User = await GetUserByIdAsync(member.UserId);
                    }
                    return members;
                }
                catch (Exception)
                {
                    return new List<FamilyMember>();
                }
            });
        }

        public async Task<IEnumerable<FamilyMember>> GetFamilyMembersByFamilyIdAsync(Guid familyId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var members = await _unitOfWork.FamilyMembers.FindAsync(fm => fm.FamilyId == familyId);
                    foreach (var member in members)
                    {
                        member.User = await GetUserByIdAsync(member.UserId);
                    }
                    return members;
                }
                catch (Exception)
                {
                    return new List<FamilyMember>();
                }
            });
        }

        public async Task<FamilyMember> GetFamilyMemberByIdAsync(Guid familyMemberId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var member = await _unitOfWork.FamilyMembers.GetByIdAsync(familyMemberId);
                    if (member != null)
                    {
                        member.User = await GetUserByIdAsync(member.UserId);
                    }
                    return member;
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task<FamilyMember> GetFamilyMemberByUserIdAsync(Guid userId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var members = await _unitOfWork.FamilyMembers.FindAsync(fm => fm.UserId == userId);
                    var member = members.FirstOrDefault();
                    if (member != null)
                    {
                        member.User = await GetUserByIdAsync(member.UserId);
                    }
                    return member;
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task<OperationResult> CreateFamilyMemberAsync(FamilyMember familyMember, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может добавлять членов семьи
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);

                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может добавлять новых членов семьи");
                    }

                    // Проверяем, существует ли пользователь
                    var user = await GetUserByIdAsync(familyMember.UserId);
                    if (user == null)
                        return OperationResult.Failure("Пользователь не найден");

                    // Проверяем, существует ли семья
                    var family = await GetFamilyByIdAsync(familyMember.FamilyId);
                    if (family == null)
                        return OperationResult.Failure("Семья не найдена");

                    // Проверяем, не состоит ли пользователь уже в этой семье
                    var existingMembers = await _unitOfWork.FamilyMembers
                        .FindAsync(fm => fm.FamilyId == familyMember.FamilyId && fm.UserId == familyMember.UserId);

                    if (existingMembers.Any())
                        return OperationResult.Failure("Пользователь уже является членом этой семьи");

                    await _unitOfWork.FamilyMembers.AddAsync(familyMember);
                    await _unitOfWork.SaveChangesAsync();

                    return OperationResult.Success("Член семьи успешно добавлен");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при добавлении члена семьи: {ex.Message}");
                }
            });
        }

        public async Task<OperationResult> ModifyFamilyMemberAsync(FamilyMember familyMember, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может редактировать членов семьи
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);

                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может редактировать данные членов семьи");
                    }

                    await _unitOfWork.FamilyMembers.UpdateAsync(familyMember);
                    return OperationResult.Success("Данные члена семьи обновлены");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при обновлении члена семьи: {ex.Message}");
                }
            });
        }

        public async Task<OperationResult> DeleteFamilyMemberAsync(Guid familyMemberId, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может удалять членов семьи
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);

                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может удалять членов семьи");
                    }

                    var member = await GetFamilyMemberByIdAsync(familyMemberId);

                    if (member == null)
                        return OperationResult.Failure("Член семьи не найден");

                    // Нельзя удалить самого себя
                    if (member.UserId == currentUserId)
                        return OperationResult.Failure("Нельзя удалить самого себя");

                    // Проверяем, не является ли пользователь единственным администратором
                    var familyMembers = await GetFamilyMembersByFamilyIdAsync(member.FamilyId);
                    var adminMembers = familyMembers.Where(fm => fm.Role == "Администратор").ToList();

                    if (adminMembers.Count == 1 && adminMembers[0].Id == member.Id)
                        return OperationResult.Failure("Нельзя удалить единственного администратора семьи");

                    await _unitOfWork.FamilyMembers.DeleteAsync(familyMemberId);
                    await _unitOfWork.SaveChangesAsync();

                    return OperationResult.Success("Член семьи успешно удален");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при удалении члена семьи: {ex.Message}");
                }
            });
        }

        #endregion

        #region Работа с категориями

        public async Task<IEnumerable<IncomeCategory>> GetIncomeCategoriesAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await _unitOfWork.IncomeCategories.GetAllAsync();
                }
                catch (Exception)
                {
                    // Возвращаем базовые категории, если не удалось загрузить из БД
                    return new List<IncomeCategory>
                    {
                        new IncomeCategory { Id = Guid.NewGuid(), Name = "Зарплата", Description = "Основная заработная плата" },
                        new IncomeCategory { Id = Guid.NewGuid(), Name = "Подработка", Description = "Дополнительный заработок" },
                        new IncomeCategory { Id = Guid.NewGuid(), Name = "Инвестиции", Description = "Доход от инвестиций" },
                        new IncomeCategory { Id = Guid.NewGuid(), Name = "Пенсия", Description = "Пенсионные выплаты" },
                        new IncomeCategory { Id = Guid.NewGuid(), Name = "Стипендия", Description = "Стипендиальные выплаты" }
                    };
                }
            });
        }

        public async Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await _unitOfWork.ExpenseCategories.GetAllAsync();
                }
                catch (Exception)
                {
                    // Возвращаем базовые категории, если не удалось загрузить из БД
                    return new List<ExpenseCategory>
                    {
                        new ExpenseCategory { Id = Guid.NewGuid(), Name = "Продукты питания", Type = "Product", Description = "Покупка продуктов" },
                        new ExpenseCategory { Id = Guid.NewGuid(), Name = "Коммунальные услуги", Type = "Service", Description = "Оплата ЖКХ" },
                        new ExpenseCategory { Id = Guid.NewGuid(), Name = "Транспорт", Type = "Service", Description = "Транспортные расходы" },
                        new ExpenseCategory { Id = Guid.NewGuid(), Name = "Образование", Type = "Service", Description = "Расходы на обучение" },
                        new ExpenseCategory { Id = Guid.NewGuid(), Name = "Развлечения", Type = "Service", Description = "Развлекательные мероприятия" },
                        new ExpenseCategory { Id = Guid.NewGuid(), Name = "Здоровье", Type = "Service", Description = "Медицинские расходы" },
                        new ExpenseCategory { Id = Guid.NewGuid(), Name = "Одежда", Type = "Product", Description = "Покупка одежды" },
                        new ExpenseCategory { Id = Guid.NewGuid(), Name = "Техника", Type = "Product", Description = "Покупка электроники" }
                    };
                }
            });
        }

        public async Task<OperationResult> AddIncomeCategoryAsync(IncomeCategory category, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может добавлять категории
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);
                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может добавлять категории доходов");
                    }

                    await _unitOfWork.IncomeCategories.AddAsync(category);
                    return OperationResult.Success("Категория доходов добавлена");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при добавлении категории доходов: {ex.Message}");
                }
            });
        }

        public async Task<OperationResult> AddExpenseCategoryAsync(ExpenseCategory category, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может добавлять категории
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);
                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может добавлять категории расходов");
                    }

                    await _unitOfWork.ExpenseCategories.AddAsync(category);
                    return OperationResult.Success("Категория расходов добавлена");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при добавлении категории расходов: {ex.Message}");
                }
            });
        }

        #endregion

        #region Работа с доходами

        public async Task<IEnumerable<Income>> GetIncomesByFamilyAsync(Guid familyId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var familyMembers = await GetFamilyMembersByFamilyIdAsync(familyId);
                    if (!familyMembers.Any())
                        return new List<Income>();

                    var memberIds = familyMembers.Select(fm => fm.Id).ToList();

                    if (startDate.HasValue && endDate.HasValue)
                    {
                        return await _unitOfWork.Incomes
                            .FindAsync(i => memberIds.Contains(i.FamilyMemberId) &&
                                           i.Date >= startDate.Value && i.Date <= endDate.Value);
                    }

                    return await _unitOfWork.Incomes
                        .FindAsync(i => memberIds.Contains(i.FamilyMemberId));
                }
                catch (Exception)
                {
                    return new List<Income>();
                }
            });
        }

        public async Task<decimal> GetTotalIncomeAsync(Guid familyId, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Получаем все доходы семьи, а не конкретного пользователя
                    var incomes = await GetIncomesByFamilyAsync(familyId, startDate, endDate);
                    return incomes.Sum(i => i.Amount);
                }
                catch (Exception)
                {
                    return 0;
                }
            });
        }

        public async Task<OperationResult> AddIncomeAsync(IncomeDto incomeDto, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверяем существование связанных сущностей
                    var familyMember = await GetFamilyMemberByIdAsync(incomeDto.FamilyMemberId);
                    if (familyMember == null)
                        return OperationResult.Failure("Выбранный член семьи не найден");

                    var category = await _unitOfWork.IncomeCategories.GetByIdAsync(incomeDto.CategoryId);
                    if (category == null)
                        return OperationResult.Failure("Выбранная категория дохода не найдена");

                    var income = new Income
                    {
                        Id = Guid.NewGuid(),
                        FamilyMemberId = incomeDto.FamilyMemberId,
                        CategoryId = incomeDto.CategoryId,
                        Amount = incomeDto.Amount,
                        Source = incomeDto.Source ?? "Не указано",
                        Description = incomeDto.Description ?? "",
                        Date = incomeDto.Date,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _unitOfWork.Incomes.AddAsync(income);
                    return OperationResult.Success("Доход успешно добавлен");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при добавлении дохода: {ex.Message}");
                }
            });
        }

        public async Task<Income> GetIncomeByIdAsync(Guid incomeId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await _unitOfWork.Incomes.GetByIdAsync(incomeId);
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task<OperationResult> UpdateIncomeAsync(IncomeDto incomeDto, Guid incomeId, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может редактировать доходы
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);
                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может редактировать доходы");
                    }

                    var existingIncome = await GetIncomeByIdAsync(incomeId);
                    if (existingIncome == null)
                        return OperationResult.Failure("Доход не найден");

                    // Проверяем существование связанных сущностей
                    var familyMember = await GetFamilyMemberByIdAsync(incomeDto.FamilyMemberId);
                    if (familyMember == null)
                        return OperationResult.Failure("Выбранный член семьи не найден");

                    var category = await _unitOfWork.IncomeCategories.GetByIdAsync(incomeDto.CategoryId);
                    if (category == null)
                        return OperationResult.Failure("Выбранная категория дохода не найдена");

                    existingIncome.FamilyMemberId = incomeDto.FamilyMemberId;
                    existingIncome.CategoryId = incomeDto.CategoryId;
                    existingIncome.Amount = incomeDto.Amount;
                    existingIncome.Source = incomeDto.Source ?? "Не указано";
                    existingIncome.Description = incomeDto.Description ?? "";
                    existingIncome.Date = incomeDto.Date;

                    await _unitOfWork.Incomes.UpdateAsync(existingIncome);
                    return OperationResult.Success("Доход успешно обновлен");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при обновлении дохода: {ex.Message}");
                }
            });
        }

        public async Task<OperationResult> DeleteIncomeAsync(Guid incomeId, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может удалять доходы
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);
                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может удалять доходы");
                    }

                    await _unitOfWork.Incomes.DeleteAsync(incomeId);
                    return OperationResult.Success("Доход удален");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при удалении дохода: {ex.Message}");
                }
            });
        }

        #endregion

        #region Работа с расходами

        public async Task<IEnumerable<Expense>> GetExpensesByFamilyAsync(Guid familyId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var familyMembers = await GetFamilyMembersByFamilyIdAsync(familyId);
                    if (!familyMembers.Any())
                        return new List<Expense>();

                    var memberIds = familyMembers.Select(fm => fm.Id).ToList();

                    if (startDate.HasValue && endDate.HasValue)
                    {
                        return await _unitOfWork.Expenses
                            .FindAsync(e => memberIds.Contains(e.FamilyMemberId) &&
                                           e.Date >= startDate.Value && e.Date <= endDate.Value);
                    }

                    return await _unitOfWork.Expenses
                        .FindAsync(e => memberIds.Contains(e.FamilyMemberId));
                }
                catch (Exception)
                {
                    return new List<Expense>();
                }
            });
        }

        public async Task<decimal> GetTotalExpenseAsync(Guid familyId, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Получаем все расходы семьи
                    var expenses = await GetExpensesByFamilyAsync(familyId, startDate, endDate);
                    return expenses.Sum(e => e.Amount);
                }
                catch (Exception)
                {
                    return 0;
                }
            });
        }

        public async Task<OperationResult> AddExpenseAsync(ExpenseDto expenseDto, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверяем существование связанных сущностей
                    var familyMember = await GetFamilyMemberByIdAsync(expenseDto.FamilyMemberId);
                    if (familyMember == null)
                        return OperationResult.Failure("Выбранный член семьи не найден");

                    var category = await _unitOfWork.ExpenseCategories.GetByIdAsync(expenseDto.CategoryId);
                    if (category == null)
                        return OperationResult.Failure("Выбранная категория расхода не найдена");

                    var expense = new Expense
                    {
                        Id = Guid.NewGuid(),
                        FamilyMemberId = expenseDto.FamilyMemberId,
                        CategoryId = expenseDto.CategoryId,
                        Amount = expenseDto.Amount,
                        Description = expenseDto.Description ?? "",
                        Date = expenseDto.Date,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _unitOfWork.Expenses.AddAsync(expense);
                    return OperationResult.Success("Расход успешно добавлен");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при добавлении расхода: {ex.Message}");
                }
            });
        }

        public async Task<Expense> GetExpenseByIdAsync(Guid expenseId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await _unitOfWork.Expenses.GetByIdAsync(expenseId);
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task<OperationResult> UpdateExpenseAsync(ExpenseDto expenseDto, Guid expenseId, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может редактировать расходы
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);
                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может редактировать расходы");
                    }

                    var existingExpense = await GetExpenseByIdAsync(expenseId);
                    if (existingExpense == null)
                        return OperationResult.Failure("Расход не найден");

                    // Проверяем существование связанных сущностей
                    var familyMember = await GetFamilyMemberByIdAsync(expenseDto.FamilyMemberId);
                    if (familyMember == null)
                        return OperationResult.Failure("Выбранный член семьи не найден");

                    var category = await _unitOfWork.ExpenseCategories.GetByIdAsync(expenseDto.CategoryId);
                    if (category == null)
                        return OperationResult.Failure("Выбранная категория расхода не найдена");

                    existingExpense.FamilyMemberId = expenseDto.FamilyMemberId;
                    existingExpense.CategoryId = expenseDto.CategoryId;
                    existingExpense.Amount = expenseDto.Amount;
                    existingExpense.Description = expenseDto.Description ?? "";
                    existingExpense.Date = expenseDto.Date;

                    await _unitOfWork.Expenses.UpdateAsync(existingExpense);
                    return OperationResult.Success("Расход успешно обновлен");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при обновлении расхода: {ex.Message}");
                }
            });
        }

        public async Task<OperationResult> DeleteExpenseAsync(Guid expenseId, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может удалять расходы
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);
                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может удалять расходы");
                    }

                    await _unitOfWork.Expenses.DeleteAsync(expenseId);
                    return OperationResult.Success("Расход удален");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при удалении расхода: {ex.Message}");
                }
            });
        }

        #endregion

        #region Работа с бюджетами

        public async Task<IEnumerable<Budget>> GetBudgetsByFamilyAsync(Guid familyId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await _unitOfWork.Budgets.FindAsync(b => b.FamilyId == familyId);
                }
                catch (Exception)
                {
                    return new List<Budget>();
                }
            });
        }

        // Добавим в FinanceService.cs:

        public async Task<Budget> GetBudgetByIdAsync(Guid budgetId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await _unitOfWork.Budgets.GetByIdAsync(budgetId);
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task<decimal> GetTotalBudgetAsync(Guid familyId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var budgets = await GetBudgetsByFamilyAsync(familyId);
                    return budgets.Sum(b => b.Amount);
                }
                catch (Exception)
                {
                    return 0;
                }
            });
        }

        public async Task<OperationResult> AddBudgetAsync(Budget budget, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может устанавливать бюджеты
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);
                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может устанавливать бюджеты");
                    }

                    // Проверяем существование связанных сущностей
                    var family = await GetFamilyByIdAsync(budget.FamilyId);
                    if (family == null)
                        return OperationResult.Failure("Семья не найдена");

                    var category = await _unitOfWork.ExpenseCategories.GetByIdAsync(budget.CategoryId);
                    if (category == null)
                        return OperationResult.Failure("Выбранная категория расхода не найдена");

                    // Проверяем, не установлен ли уже бюджет для этой категории на этот период
                    var existingBudgets = await _unitOfWork.Budgets
                        .FindAsync(b => b.FamilyId == budget.FamilyId &&
                                       b.CategoryId == budget.CategoryId &&
                                       ((b.StartDate <= budget.EndDate && b.EndDate >= budget.StartDate)));

                    if (existingBudgets.Any())
                        return OperationResult.Failure("Бюджет для этой категории на этот период уже установлен");

                    await _unitOfWork.Budgets.AddAsync(budget);
                    return OperationResult.Success("Бюджет успешно установлен");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при установке бюджета: {ex.Message}");
                }
            });
        }

        public async Task<OperationResult> UpdateBudgetAsync(Budget budget, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может обновлять бюджеты
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);
                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может обновлять бюджеты");
                    }

                    await _unitOfWork.Budgets.UpdateAsync(budget);
                    return OperationResult.Success("Бюджет обновлен");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при обновлении бюджета: {ex.Message}");
                }
            });
        }

        public async Task<OperationResult> DeleteBudgetAsync(Guid budgetId, Guid currentUserId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Проверка прав: только администратор может удалять бюджеты
                    var currentUserMember = await GetFamilyMemberByUserIdAsync(currentUserId);
                    if (currentUserMember?.Role != "Администратор")
                    {
                        return OperationResult.Failure("Только администратор может удалять бюджеты");
                    }

                    await _unitOfWork.Budgets.DeleteAsync(budgetId);
                    return OperationResult.Success("Бюджет удален");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure($"Ошибка при удалении бюджета: {ex.Message}");
                }
            });
        }

        #endregion

        #region Финансовая статистика и отчеты

        public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(Guid familyId, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    // Считаем доходы и расходы ВСЕЙ СЕМЬИ
                    var totalIncome = await GetTotalIncomeAsync(familyId, startDate, endDate);
                    var totalExpense = await GetTotalExpenseAsync(familyId, startDate, endDate);
                    var totalBudget = await GetTotalBudgetAsync(familyId);

                    return new FinancialSummaryDto
                    {
                        TotalIncome = totalIncome,
                        TotalExpense = totalExpense,
                        Balance = totalIncome - totalExpense,
                        BudgetTotal = totalBudget,
                        PeriodStart = startDate,
                        PeriodEnd = endDate
                    };
                }
                catch (Exception)
                {
                    return new FinancialSummaryDto
                    {
                        TotalIncome = 0,
                        TotalExpense = 0,
                        Balance = 0,
                        BudgetTotal = 0,
                        PeriodStart = startDate,
                        PeriodEnd = endDate
                    };
                }
            });
        }

        public async Task<List<ReportItemDto>> GetFinancialReportAsync(Guid familyId, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var reportItems = new List<ReportItemDto>();

                    // Получение доходов
                    var incomes = await GetIncomesByFamilyAsync(familyId, startDate, endDate);
                    foreach (var income in incomes)
                    {
                        var category = await _unitOfWork.IncomeCategories.GetByIdAsync(income.CategoryId);
                        var familyMember = await GetFamilyMemberByIdAsync(income.FamilyMemberId);
                        var user = familyMember != null ? await GetUserByIdAsync(familyMember.UserId) : null;

                        reportItems.Add(new ReportItemDto
                        {
                            Id = income.Id,
                            Date = income.Date,
                            OperationType = "Доход",
                            Category = category?.Name ?? "Без категории",
                            Amount = income.Amount,
                            Description = income.Description ?? "",
                            Username = user?.Username ?? "Неизвестно",
                            FamilyMemberRole = familyMember?.Role ?? "Не указано",
                            Source = income.Source ?? ""
                        });
                    }

                    // Получение расходов
                    var expenses = await GetExpensesByFamilyAsync(familyId, startDate, endDate);
                    foreach (var expense in expenses)
                    {
                        var category = await _unitOfWork.ExpenseCategories.GetByIdAsync(expense.CategoryId);
                        var familyMember = await GetFamilyMemberByIdAsync(expense.FamilyMemberId);
                        var user = familyMember != null ? await GetUserByIdAsync(familyMember.UserId) : null;

                        reportItems.Add(new ReportItemDto
                        {
                            Id = expense.Id,
                            Date = expense.Date,
                            OperationType = "Расход",
                            Category = category?.Name ?? "Без категории",
                            Amount = expense.Amount,
                            Description = expense.Description ?? "",
                            Username = user?.Username ?? "Неизвестно",
                            FamilyMemberRole = familyMember?.Role ?? "Не указано",
                            Source = ""
                        });
                    }

                    return reportItems.OrderByDescending(r => r.Date).ToList();
                }
                catch (Exception)
                {
                    return new List<ReportItemDto>();
                }
            });
        }

        public async Task<Dictionary<string, decimal>> GetCategoryExpensesAsync(Guid familyId, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var expenses = await GetExpensesByFamilyAsync(familyId, startDate, endDate);
                    var result = new Dictionary<string, decimal>();

                    foreach (var expense in expenses)
                    {
                        var category = await _unitOfWork.ExpenseCategories.GetByIdAsync(expense.CategoryId);
                        var categoryName = category?.Name ?? "Без категории";

                        if (result.ContainsKey(categoryName))
                            result[categoryName] += expense.Amount;
                        else
                            result[categoryName] = expense.Amount;
                    }

                    return result;
                }
                catch (Exception)
                {
                    return new Dictionary<string, decimal>();
                }
            });
        }

        public async Task<IEnumerable<BudgetAlert>> CheckBudgetAlertsAsync(Guid familyId)
        {
            try
            {
                var alerts = new List<BudgetAlert>();
                var budgets = await GetBudgetsByFamilyAsync(familyId);

                foreach (var budget in budgets)
                {
                    var category = await _unitOfWork.ExpenseCategories.GetByIdAsync(budget.CategoryId);
                    var expenses = await GetExpensesByFamilyAsync(familyId, budget.StartDate, budget.EndDate);
                    var categoryExpenses = expenses
                        .Where(e => e.CategoryId == budget.CategoryId)
                        .Sum(e => e.Amount);

                    if (categoryExpenses > budget.Amount)
                    {
                        alerts.Add(new BudgetAlert
                        {
                            Id = Guid.NewGuid(),
                            BudgetId = budget.Id,
                            Message = $"Превышен бюджет по категории '{category?.Name}'. " +
                                     $"Установлено: {budget.Amount:C}, потрачено: {categoryExpenses:C}",
                            AlertDate = DateTime.UtcNow,
                            IsRead = false
                        });
                    }
                    else if (categoryExpenses > budget.Amount * 0.8m) // 80% бюджета
                    {
                        alerts.Add(new BudgetAlert
                        {
                            Id = Guid.NewGuid(),
                            BudgetId = budget.Id,
                            Message = $"Близко к превышению бюджета по категории '{category?.Name}'. " +
                                     $"Установлено: {budget.Amount:C}, потрачено: {categoryExpenses:C}",
                            AlertDate = DateTime.UtcNow,
                            IsRead = false
                        });
                    }
                }

                return alerts;
            }
            catch (Exception)
            {
                return new List<BudgetAlert>();
            }
        }

        #endregion

        #region Бюджеты с деталями

        public async Task<IEnumerable<BudgetDto>> GetBudgetsWithDetailsAsync(Guid familyId)
        {
            try
            {
                var budgets = await GetBudgetsByFamilyAsync(familyId);
                var result = new List<BudgetDto>();

                foreach (var budget in budgets)
                {
                    var category = await _unitOfWork.ExpenseCategories.GetByIdAsync(budget.CategoryId);
                    // Рассчитываем потраченную сумму за период бюджета
                    var expenses = await GetExpensesByFamilyAsync(familyId, budget.StartDate, budget.EndDate);
                    var categoryExpenses = expenses
                        .Where(e => e.CategoryId == budget.CategoryId)
                        .Sum(e => e.Amount);

                    result.Add(new BudgetDto
                    {
                        Id = budget.Id,
                        CategoryName = category?.Name ?? "Неизвестно",
                        Amount = budget.Amount,
                        PeriodType = budget.PeriodType,
                        StartDate = budget.StartDate,
                        EndDate = budget.EndDate,
                        CurrentSpent = categoryExpenses
                    });
                }

                return result;
            }
            catch (Exception)
            {
                return new List<BudgetDto>();
            }
        }

        public async Task<IEnumerable<BudgetAlertDto>> GetBudgetAlertsAsync(Guid familyId)
        {
            try
            {
                var budgets = await GetBudgetsByFamilyAsync(familyId);
                var alerts = new List<BudgetAlertDto>();

                foreach (var budget in budgets)
                {
                    var category = await _unitOfWork.ExpenseCategories.GetByIdAsync(budget.CategoryId);
                    var expenses = await GetExpensesByFamilyAsync(familyId, budget.StartDate, budget.EndDate);
                    var categoryExpenses = expenses
                        .Where(e => e.CategoryId == budget.CategoryId)
                        .Sum(e => e.Amount);

                    // Проверяем, что бюджет не нулевой, чтобы избежать деления на ноль
                    if (budget.Amount == 0) continue;

                    if (categoryExpenses > budget.Amount * 0.8m) // Более 80%
                    {
                        var alert = new BudgetAlertDto
                        {
                            Id = Guid.NewGuid(),
                            BudgetId = budget.Id,
                            CategoryName = category?.Name ?? "Неизвестно",
                            Message = categoryExpenses > budget.Amount
                                ? $"⚠️ Превышен бюджет по категории '{category?.Name}'! Установлено: {budget.Amount:C}, потрачено: {categoryExpenses:C}"
                                : $"⚠️ Близко к превышению бюджета по категории '{category?.Name}'. Установлено: {budget.Amount:C}, потрачено: {categoryExpenses:C} ({categoryExpenses / budget.Amount * 100:F0}%)",
                            AlertDate = DateTime.UtcNow,
                            BudgetAmount = budget.Amount,
                            CurrentSpent = categoryExpenses,
                            Percentage = (double)(categoryExpenses / budget.Amount * 100),
                            IsCritical = categoryExpenses > budget.Amount
                        };
                        alerts.Add(alert);
                    }
                }

                return alerts.OrderByDescending(a => a.IsCritical).ThenByDescending(a => a.Percentage);
            }
            catch (Exception)
            {
                return new List<BudgetAlertDto>();
            }
        }

        #endregion

        #region Вспомогательные методы

        public async Task<decimal> GetFamilyBalanceAsync(Guid familyId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var summary = await GetFinancialSummaryAsync(familyId, DateTime.MinValue, DateTime.MaxValue);
                    return summary.Balance;
                }
                catch (Exception)
                {
                    return 0;
                }
            });
        }

        public async Task<bool> IsFamilyMemberAsync(Guid familyId, Guid userId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var members = await GetFamilyMembersByFamilyIdAsync(familyId);
                    return members.Any(m => m.UserId == userId);
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }


        #endregion
    }
}