using Demo.Application.Services;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

namespace Demo.Infra.Extensions;

/// <summary>
/// 데이터베이스 작업에 대한 OpenTelemetry 계측을 위한 확장 메서드 클래스
/// </summary>
public static class DatabaseInstrumentationExtensions
{
    /// <summary>
    /// 데이터베이스 작업을 계측하여 실행합니다.
    /// </summary>
    /// <typeparam name="T">반환 타입</typeparam>
    /// <param name="telemetryService">텔레메트리 서비스</param>
    /// <param name="logger">로거</param>
    /// <param name="operationName">작업 이름</param>
    /// <param name="dbSystem">데이터베이스 시스템 (예: postgresql, mysql)</param>
    /// <param name="dbOperation">데이터베이스 작업 (예: SELECT, INSERT, UPDATE, DELETE)</param>
    /// <param name="tableName">테이블 이름</param>
    /// <param name="repositoryClass">리포지토리 클래스 이름</param>
    /// <param name="methodName">메서드 이름</param>
    /// <param name="operation">실행할 데이터베이스 작업</param>
    /// <param name="additionalTags">추가 태그</param>
    /// <summary>
    /// Executes a database operation asynchronously with OpenTelemetry instrumentation, capturing execution timing, metadata, and error information.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
    /// <param name="operationName">A descriptive name for the database operation.</param>
    /// <param name="dbSystem">The type of database system (e.g., "postgresql", "mysql").</param>
    /// <param name="dbOperation">The specific database operation being performed (e.g., "SELECT", "INSERT").</param>
    /// <param name="tableName">The name of the database table involved in the operation.</param>
    /// <param name="repositoryClass">The name of the repository class initiating the operation.</param>
    /// <param name="methodName">The name of the method in the repository class.</param>
    /// <param name="operation">A delegate representing the asynchronous database operation to execute. Receives the current Activity as a parameter.</param>
    /// <param name="additionalTags">Optional additional tags to include in the telemetry data.</param>
    /// <returns>The result of the database operation.</returns>
    public static async Task<T> InstrumentDatabaseOperationAsync<T>(
        this ITelemetryService telemetryService,
        ILogger logger,
        string operationName,
        string dbSystem,
        string dbOperation,
        string tableName,
        string repositoryClass,
        string methodName,
        Func<Activity?, Task<T>> operation,
        Dictionary<string, object?>? additionalTags = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // 기본 태그 설정
        var tags = new Dictionary<string, object?>
        {
            ["db.system"] = dbSystem,
            ["db.operation"] = dbOperation,
            ["db.table"] = tableName,
            ["repository.class"] = repositoryClass,
            ["repository.method"] = methodName
        };

        // 추가 태그 병합
        if (additionalTags != null)
        {
            foreach (var tag in additionalTags)
            {
                tags[tag.Key] = tag.Value;
            }
        }

        // Activity 시작
        using var activity = telemetryService.StartActivity(operationName, tags);

        try
        {
            // 작업 시작 로그
            telemetryService.LogInformationWithTrace(logger, 
                "데이터베이스 작업 시작 - Operation: {Operation}, Table: {Table}", 
                dbOperation, tableName);

            // 실제 데이터베이스 작업 실행
            var result = await operation(activity);
            
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalMilliseconds;

            // Activity에 성능 정보 추가
            activity?.SetTag("db.total_duration_ms", duration);

            // 성공 로그
            telemetryService.LogInformationWithTrace(logger, 
                "데이터베이스 작업 완료 - Operation: {Operation}, Table: {Table}, Duration: {Duration}ms", 
                dbOperation, tableName, duration);

            return result;
        }
        catch (Exception e)
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalMilliseconds;

            // Activity에 예외 정보 설정
            telemetryService.SetActivityError(activity, e);
            activity?.SetTag("db.total_duration_ms", duration);

            // 예외 메트릭 기록
            telemetryService.RecordError(e.GetType().Name, operationName, e.Message);

            // 예외 로그
            telemetryService.LogErrorWithTrace(logger, e, 
                "데이터베이스 작업 중 예외 발생 - Operation: {Operation}, Table: {Table}", 
                dbOperation, tableName);

            throw;
        }
    }

    /// <summary>
    /// 데이터베이스 연결 정보를 Activity에 추가합니다.
    /// </summary>
    /// <param name="activity">Activity 인스턴스</param>
    /// <param name="connection">데이터베이스 연결</param>
    public static void AddConnectionInfo(this Activity? activity, IDbConnection connection)
    {
        if (activity == null) return;

        // 연결 문자열에서 안전한 정보만 추출
        var connectionString = connection.ConnectionString;
        if (string.IsNullOrEmpty(connectionString)) return;

        try
        {
            // PostgreSQL 연결 문자열 파싱 (Npgsql)
            if (connection.GetType().Name.Contains("Npgsql"))
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                activity.SetTag("db.connection_string.host", builder.Host);
                activity.SetTag("db.connection_string.database", builder.Database);
                activity.SetTag("db.connection_string.port", builder.Port);
                activity.SetTag("db.connection_string.username", builder.Username);
            }
            // 다른 데이터베이스 타입에 대한 처리도 여기에 추가 가능
        }
        catch (Exception)
        {
            // 연결 문자열 파싱 실패 시 무시
        }
    }

    /// <summary>
    /// SQL 쿼리 정보를 Activity에 추가합니다 (민감한 데이터 제외).
    /// </summary>
    /// <param name="activity">Activity 인스턴스</param>
    /// <param name="sqlQuery">SQL 쿼리</param>
    /// <param name="parameterCount">매개변수 개수</param>
    public static void AddSqlQueryInfo(this Activity? activity, string sqlQuery, int parameterCount = 0)
    {
        if (activity == null || string.IsNullOrEmpty(sqlQuery)) return;

        // 매개변수 값을 제거한 안전한 쿼리 문자열 생성
        var safeSqlQuery = SanitizeSqlQuery(sqlQuery);
        
        activity.SetTag("db.statement", safeSqlQuery);
        activity.SetTag("db.parameter_count", parameterCount);
    }

    /// <summary>
    /// SQL 쿼리에서 민감한 데이터를 제거합니다.
    /// </summary>
    /// <param name="sqlQuery">원본 SQL 쿼리</param>
    /// <returns>정리된 SQL 쿼리</returns>
    private static string SanitizeSqlQuery(string sqlQuery)
    {
        if (string.IsNullOrEmpty(sqlQuery)) return sqlQuery;

        // 매개변수 값을 플레이스홀더로 대체
        var sanitized = sqlQuery;
        
        // @parameter 형태의 매개변수를 ?로 대체
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, @"@\w+", "?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // 문자열 리터럴을 ?로 대체
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, @"'[^']*'", "?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // 숫자 리터럴을 ?로 대체
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, @"\b\d+\b", "?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return sanitized;
    }

    /// <summary>
    /// 쿼리 실행 결과 정보를 Activity에 추가합니다.
    /// </summary>
    /// <param name="activity">Activity 인스턴스</param>
    /// <param name="rowsAffected">영향받은 행 수</param>
    /// <param name="queryDuration">쿼리 실행 시간 (밀리초)</param>
    public static void AddQueryResultInfo(this Activity? activity, int rowsAffected, double queryDuration)
    {
        if (activity == null) return;

        activity.SetTag("db.rows_affected", rowsAffected);
        activity.SetTag("db.query_duration_ms", queryDuration);
    }

    /// <summary>
    /// 조회 결과 정보를 Activity에 추가합니다.
    /// </summary>
    /// <param name="activity">Activity 인스턴스</param>
    /// <param name="rowsReturned">반환된 행 수</param>
    /// <param name="queryDuration">쿼리 실행 시간 (밀리초)</param>
    public static void AddSelectResultInfo(this Activity? activity, int rowsReturned, double queryDuration)
    {
        if (activity == null) return;

        activity.SetTag("db.rows_returned", rowsReturned);
        activity.SetTag("db.query_duration_ms", queryDuration);
    }
}