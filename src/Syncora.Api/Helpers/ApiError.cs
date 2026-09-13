namespace Syncora.Helpers
{
    /// <summary>
    /// Стандартизированный формат ошибок API (ТЗ §22):
    /// { "error": { "code": "...", "message": "..." } }.
    /// </summary>
    public static class ApiError
    {
        public static object Body(string code, string message) =>
            new { error = new { code, message } };
    }
}
