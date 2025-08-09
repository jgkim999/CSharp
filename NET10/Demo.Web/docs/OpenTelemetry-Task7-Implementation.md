# OpenTelemetry 데이터베이스 계측 구현 가이드

## 개요

이 문서는 Demo.Web 프로젝트에서 데이터베이스 작업에 대한 OpenTelemetry 계측을 구현한 내용을 설명합니다. PostgreSQL과 Dapper를 사용하는 환경에서 자동 계측과 사용자 정의 계측을 모두 구현했습니다.

## 구현된 기능

### 7.1 데이터베이스 자동 계측

#### Npgsql 자동 계측
- **위치**: `NET10/Demo.Web/Extensions/OpenTelemetryExtensions.cs`
- **기능**: PostgreSQL 연결 및 쿼리 자동 추적
- **구현 내용**:
  ```csharp
  // PostgreSQL (Npgsql) 계측
  .AddNpgsql()
  ```

#### SqlClient 자동 계측
- **기능**: SQL 명령문 자동 추적 및 성능 모니터링
- **구현 내용**:
  ```csharp
  .AddSqlClientInstrumentation(options =>
  {
      // SQL 명령문 텍스트 기록 (개발 환경에서만)
      options.SetDbStatementForText = true;
      options.SetDbStatementForStoredProcedure = true;
      options.RecordException = true;
      
      // SQL 명령문 필터링 (민감한 정보 제외)
      options.Filter = command =>
      {
          var commandText = command.CommandText?.ToLowerInvariant();
          return !string.IsNullOrEmpty(commandText) && 
                 !commandText.Contains("pg_stat") && 
                 !commandText.Contains("information_schema");
      };

      // 개발 환경에서만 SQL 문 기록
      options.SetDbStatementForText = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
  })
  ```

### 7.2 사용자 정의 데이터베이스 작업 추적

#### 데이터베이스 계측 확장 메서드
- **위치**: `NET10/Demo.Infra/Extensions/DatabaseInstrumentationExtensions.cs`
- **목적**: 데이터베이스 작업에 대한 일관된 계측 제공

##### 주요 기능

1. **InstrumentDatabaseOperationAsync**: 데이터베이스 작업 전체를 계측
2. **AddConnectionInfo**: 데이터베이스 연결 정보를 Activity에 추가
3. **AddSqlQueryInfo**: SQL 쿼리 정보를 안전하게 Activity에 추가
4. **AddQueryResultInfo**: 쿼리 실행 결과 정보 추가
5. **AddSelectResultInfo**: 조회 결과 정보 추가

##### 보안 기능
- **SQL 쿼리 정리**: 매개변수 값을 플레이스홀더로 대체하여 민감한 데이터 보호
- **연결 문자열 파싱**: 안전한 정보만 추출하여 Activity에 추가

#### UserRepositoryPostgre 리팩토링
- **위치**: `NET10/Demo.Infra/Repositories/UserRepositoryPostgre.cs`
- **개선 사항**:
  - 새로운 확장 메서드를 사용하여 코드 간소화
  - 일관된 계측 패턴 적용
  - 성능 측정 및 오류 처리 개선

## 수집되는 텔레메트리 데이터

### Activity 태그 (Tags)

#### 기본 데이터베이스 정보
- `db.system`: 데이터베이스 시스템 (예: "postgresql")
- `db.operation`: 데이터베이스 작업 (예: "SELECT", "INSERT", "UPDATE", "DELETE")
- `db.table`: 대상 테이블 이름
- `db.statement`: 정리된 SQL 쿼리 (매개변수 값 제외)
- `db.parameter_count`: SQL 매개변수 개수

#### 연결 정보
- `db.connection_string.host`: 데이터베이스 호스트
- `db.connection_string.database`: 데이터베이스 이름
- `db.connection_string.port`: 데이터베이스 포트
- `db.connection_string.username`: 사용자명

#### 성능 정보
- `db.query_duration_ms`: 쿼리 실행 시간 (밀리초)
- `db.total_duration_ms`: 전체 작업 시간 (밀리초)
- `db.rows_affected`: 영향받은 행 수 (INSERT, UPDATE, DELETE)
- `db.rows_returned`: 반환된 행 수 (SELECT)
- `mapping.duration_ms`: 객체 매핑 시간 (밀리초)

#### 리포지토리 정보
- `repository.class`: 리포지토리 클래스 이름
- `repository.method`: 실행된 메서드 이름

#### 오류 정보 (오류 발생 시)
- `error`: true
- `error.type`: 오류 유형
- `error.message`: 오류 메시지

### 메트릭

#### 성공 메트릭
- `db_user_create_success`: 사용자 생성 성공 횟수
- `db_user_getall_success`: 사용자 조회 성공 횟수

#### 실패 메트릭
- `db_user_create_failures`: 사용자 생성 실패 횟수
- `db_user_create_exceptions`: 사용자 생성 예외 횟수
- `db_user_getall_exceptions`: 사용자 조회 예외 횟수
- `db_user_getall_mapping_errors`: 매핑 오류 횟수

## 사용 예시

### 기본 사용법
```csharp
public async Task<Result> CreateAsync(string name, string email, string passwordSha256)
{
    return await _telemetryService.InstrumentDatabaseOperationAsync(
        _logger,
        "db.user.create",           // 작업 이름
        "postgresql",               // DB 시스템
        "INSERT",                   // DB 작업
        "users",                    // 테이블 이름
        nameof(UserRepositoryPostgre), // 리포지토리 클래스
        nameof(CreateAsync),        // 메서드 이름
        async (activity) =>
        {
            // 실제 데이터베이스 작업 구현
            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            
            // 연결 정보 추가
            activity.AddConnectionInfo(connection);
            
            // SQL 쿼리 정보 추가
            activity.AddSqlQueryInfo(sqlQuery, parameterCount);
            
            // 쿼리 실행
            var result = await connection.ExecuteAsync(sqlQuery, parameters);
            
            // 결과 정보 추가
            activity.AddQueryResultInfo(result, queryDuration);
            
            return Result.Ok();
        },
        new Dictionary<string, object?> // 추가 태그
        {
            ["db.user.email"] = email,
            ["db.user.name"] = name
        });
}
```

## 보안 고려사항

### 민감한 데이터 보호
1. **SQL 매개변수 값**: 실제 값 대신 플레이스홀더(`?`) 사용
2. **연결 문자열**: 비밀번호 등 민감한 정보 제외
3. **사용자 데이터**: 필요한 경우에만 안전한 식별자 사용

### 개발/프로덕션 환경 구분
- **개발 환경**: 상세한 SQL 문 기록 활성화
- **프로덕션 환경**: 민감한 정보 기록 비활성화

## 성능 최적화

### 샘플링
- 높은 트래픽 상황에서 성능 영향 최소화
- 환경별 샘플링 비율 조정

### 배치 처리
- 메트릭 데이터의 배치 처리로 효율성 향상
- 적절한 내보내기 간격 설정

## 모니터링 및 알림

### 주요 모니터링 지표
1. **응답 시간**: 데이터베이스 쿼리 실행 시간
2. **오류율**: 데이터베이스 작업 실패율
3. **처리량**: 초당 처리되는 쿼리 수
4. **연결 상태**: 데이터베이스 연결 상태

### 알림 설정 권장사항
- 쿼리 실행 시간이 임계값 초과 시 알림
- 데이터베이스 연결 실패 시 즉시 알림
- 오류율이 임계값 초과 시 알림

## 문제 해결

### 일반적인 문제

1. **Activity가 생성되지 않는 경우**
   - ActivitySource가 올바르게 등록되었는지 확인
   - 샘플링 설정 확인

2. **SQL 문이 기록되지 않는 경우**
   - 환경 변수 `ASPNETCORE_ENVIRONMENT` 확인
   - `SetDbStatementForText` 설정 확인

3. **연결 정보가 누락되는 경우**
   - 연결 문자열 형식 확인
   - 데이터베이스 드라이버 호환성 확인

### 디버깅 팁
- 콘솔 익스포터를 사용하여 로컬에서 텔레메트리 데이터 확인
- 로그 레벨을 Debug로 설정하여 상세 정보 확인
- Activity 태그를 통해 실행 경로 추적

## 향후 개선 사항

1. **추가 데이터베이스 지원**: MySQL, SQL Server 등
2. **고급 쿼리 분석**: 느린 쿼리 자동 감지
3. **연결 풀 모니터링**: 연결 풀 상태 추적
4. **자동 성능 튜닝**: 쿼리 성능 기반 자동 최적화 제안

## 결론

이번 구현을 통해 데이터베이스 작업에 대한 포괄적인 관찰 가능성을 확보했습니다. 자동 계측과 사용자 정의 계측을 조합하여 성능 모니터링, 오류 추적, 보안 고려사항을 모두 충족하는 솔루션을 구축했습니다.