using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Extensions;

public static class WithCallerErrorLogger
{
    public static void LogErrorWithCaller<T>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] {Message}",
            fileName, memberName, sourceLineNumber, message);
    }
    
    public static void LogErrorWithCaller<T>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] {Message}",
            fileName, memberName, sourceLineNumber, message);
    }

    public static void LogErrorWithCaller<T, TArg>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg arg,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1,
        TArg2 arg2,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TArg17>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16, TArg17 arg17,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TArg17, TArg18>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16, TArg17 arg17, TArg18 arg18,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TArg17, TArg18, TArg19>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16, TArg17 arg17, TArg18 arg18, TArg19 arg19,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TArg17, TArg18, TArg19, TArg20>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16, TArg17 arg17, TArg18 arg18, TArg19 arg19, TArg20 arg20,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError("[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, arg20);
    }

    #region Exception 오버로드 (0-20 파라미터)

    public static void LogErrorWithCaller<T, TArg>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg arg,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1,
        TArg2 arg2,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TArg17>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16, TArg17 arg17,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TArg17, TArg18>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16, TArg17 arg17, TArg18 arg18,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TArg17, TArg18, TArg19>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16, TArg17 arg17, TArg18 arg18, TArg19 arg19,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19);
    }

    public static void LogErrorWithCaller<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TArg17, TArg18, TArg19, TArg20>(
        this Microsoft.Extensions.Logging.ILogger<T> logger,
        Exception exception,
        string messageTemplate,
        TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16, TArg17 arg17, TArg18 arg18, TArg19 arg19, TArg20 arg20,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;
        var fileName = Path.GetFileName(sourceFilePath);
        logger.LogError(exception, "[{FileName}:{MemberName}:{LineNumber}] " + messageTemplate,
            fileName, memberName, sourceLineNumber, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, arg20);
    }

    #endregion
}