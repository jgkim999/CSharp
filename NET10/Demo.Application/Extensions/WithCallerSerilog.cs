using System.Runtime.CompilerServices;

namespace Demo.Application.Extensions;

public static class WithCallerSerilog
{
    public static Serilog.ILogger WithCallerInfo(
        this Serilog.ILogger logger,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        // 보안을 위해 전체 경로 대신 파일 이름만 사용
        var fileName = Path.GetFileName(sourceFilePath);

        return logger
            .ForContext("FileName", fileName)
            .ForContext("MemberName", memberName)
            .ForContext("LineNumber", sourceLineNumber);
    }
}
