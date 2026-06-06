namespace Demo.Application.Exceptions;

public class BusinessException : Exception
{
    // 1. 기본 생성자
    public BusinessException() : base() { }

    // 2. 메시지를 받는 생성자 (가장 많이 사용)
    public BusinessException(string message) : base(message) { }

    // 3. 메시지와 내부 예외(InnerException)를 함께 받는 생성자
    public BusinessException(string message, Exception innerException)
        : base(message, innerException) { }
}
