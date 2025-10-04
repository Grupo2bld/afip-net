namespace bk_arca.DTOs
{
    public class ResponseDto
    {

        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public ResponseDto() { }
        public ResponseDto(bool success, string message, object? data = null)
        {
            Success = success;
            Message = message;
            Data = data;
        }

    }
}
