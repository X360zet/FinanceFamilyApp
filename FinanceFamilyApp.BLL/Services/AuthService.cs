using FinanceFamilyApp.BLL.DTO;
using FinanceFamilyApp.DAL;
using FinanceFamilyApp.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceFamilyApp.BLL.Services
{
    public class AuthService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            try
            {
                var users = await _unitOfWork.Users.FindAsync(u => u.Username == username);
                var user = users.FirstOrDefault();

                if (user != null)
                {
                    // Проверяем пароль с помощью BCrypt
                    bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                    if (isValid)
                    {
                        return user;
                    }
                    else
                    {
                        // Для отладки - посмотрим, что хранится в базе
                        Console.WriteLine($"Введенный пароль: {password}");
                        Console.WriteLine($"Хэш из БД: {user.PasswordHash}");
                        Console.WriteLine($"Проверка BCrypt: {isValid}");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                // Логируем ошибку для отладки
                Console.WriteLine($"Ошибка аутентификации: {ex.Message}");
                return null;
            }
        }

        public async Task<OperationResult> RegisterAsync(string username, string email, string password)
        {
            try
            {
                // Проверяем, существует ли пользователь
                var existingUsers = await _unitOfWork.Users.FindAsync(u => u.Username == username || u.Email == email);

                if (existingUsers.Any())
                {
                    return OperationResult.Failure("Пользователь с таким именем или email уже существует");
                }

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    CreatedDate = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);

                // Создаем семью для нового пользователя
                var family = new Family
                {
                    Id = Guid.NewGuid(),
                    Name = $"Семья {username}",
                    CreatedDate = DateTime.UtcNow
                };

                await _unitOfWork.Families.AddAsync(family);

                // Добавляем пользователя как АДМИНИСТРАТОРА семьи
                var familyMember = new FamilyMember
                {
                    Id = Guid.NewGuid(),
                    FamilyId = family.Id,
                    UserId = user.Id,
                    Role = "Администратор", // Установка роли Администратор
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                await _unitOfWork.FamilyMembers.AddAsync(familyMember);

                return OperationResult.Success("Пользователь успешно зарегистрирован");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Ошибка регистрации: {ex.Message}");
            }
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            return await _unitOfWork.Users.GetByIdAsync(userId);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Username == username);
            return users.FirstOrDefault();
        }
        public async Task<OperationResult> RegisterFamilyMemberAsync(string username, string email, string password, string role)
        {
            try
            {
                // Проверяем, существует ли пользователь
                var existingUsers = await _unitOfWork.Users.FindAsync(u => u.Username == username || u.Email == email);

                if (existingUsers.Any())
                {
                    return OperationResult.Failure("Пользователь с таким именем или email уже существует");
                }

                // Создаем пользователя
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    CreatedDate = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync(); // Важно сохранить изменения!

                return OperationResult.Success("Пользователь успешно создан");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Ошибка регистрации: {ex.Message}");
            }
        }
    }

}
