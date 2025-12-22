namespace FinanceFamilyApp.BLL.DTO
{
    public class OperationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }

        public static OperationResult Success(string message = "Успешно")
        {
            return new OperationResult { IsSuccess = true, Message = message };
        }

        public static OperationResult Failure(string message)
        {
            return new OperationResult { IsSuccess = false, Message = message };
        }
    }
}