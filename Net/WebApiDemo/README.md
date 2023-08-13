# 목차

- [목차](#목차)
  - [문서화](#문서화)
  - [로그](#로그)
  - [헬스체크](#헬스체크)
  - [IP별 요청 제한 기능](#ip별-요청-제한-기능)
  - [글로벌 에러 핸들링](#글로벌-에러-핸들링)
  - [Http 로깅](#http-로깅)
  - [요청 응답 미들웨어](#요청-응답-미들웨어)
  - [스케쥴링](#스케쥴링)
  - [고유키 발급](#고유키-발급)
  - [TODO](#todo)
    - [Bogus](#bogus)
    - [Hashids](#hashids)
    - [RestSharp](#restsharp)
    - [Flurl](#flurl)
    - [Polly](#polly)
    - [NMemory](#nmemory)
    - [LazyCache](#lazycache)
    - [FluentValidation](#fluentvalidation)
    - [FluentAssertions](#fluentassertions)
    - [Noda Time](#noda-time)
    - [Humanizer](#humanizer)
    - [MediatR](#mediatr)
    - [AppMetrics](#appmetrics)
    - [BenchmarkDotNet](#benchmarkdotnet)
  - [사용한 Nuget 패키지](#사용한-nuget-패키지)
  - [MySQL](#mysql)

Fork해서 사용하세요.

소스코드에 아래 기능 및 예제가 모두 포함되어있습니다.

WebAPI 프로젝트 시작시 참고하시면 됩니다.

.Net7 과 Visual Studio 2022로 작성되었습니다.

## 문서화

Swagger를 통해서 문서화를 합니다.

![Swagger](./Images/swagger.png)

[Swagger](https://swagger.io/)

[API 문서화](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-7.0&tabs=visual-studio)

## 로그

Serilog를 비동기적으로 이용합니다.

[Serilog](https://serilog.net/)

[Serilog-sinks-async](https://github.com/serilog/serilog-sinks-async)

## 헬스체크

[Health checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-7.0)

[healthz API](http://localhost/healthz)

[Health check UI](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)

![Health check UI Image](./Images/health_check_ui.png)

[http://localhost/healthchecks-ui](http://localhost/healthchecks-ui)

## IP별 요청 제한 기능

[Rate limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-7.0)

[![Watch the video]](https://www.youtube.com/watch?v=PIfGHbvuAtM&t=369s&ab_channel=MilanJovanovi%C4%87)

## 글로벌 에러 핸들링

[Global Error Handling](https://code-maze.com/global-error-handling-aspnetcore/)

[![Watch the video]](https://www.youtube.com/watch?v=tk1QK71DVtg&ab_channel=CodeMaze)

## Http 로깅

[Http Logging](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-logging/?view=aspnetcore-7.0)

## 요청 응답 미들웨어

[ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-7.0)

## 스케쥴링

[Quartz.NET](https://www.quartz-scheduler.net/)

## 고유키 발급

.NET Core 및 Unity용 ULID의 빠른 C# 구현.

ULID는 정렬 가능한 랜덤 아이디 생성기입니다.

각종 고유번호 생성에 좋음, 시간값으로 증가하기 때문에 DB에 넣어도 인덱스 단편화가 방지됨

다른 머신에서 생성해도 고유값이 보장됨.

[Ulid Spec](https://github.com/ulid/spec)

[Cysharp Ulid](https://github.com/Cysharp/Ulid)

## TODO

### Bogus

[Bogus for .NET](https://github.com/bchavez/Bogus)

### Hashids

[Hashids.net](https://github.com/ullmark/hashids.net)

### RestSharp

[RestSharp](https://restsharp.dev/)

### Flurl

[Flurl](https://flurl.dev/)

### Polly

[Polly](https://github.com/App-vNext/Polly)

### NMemory

[NMemory](https://nmemory.net/)

### LazyCache

[LazyCache](https://github.com/alastairtree/LazyCache)

### FluentValidation

[FluentValidation](https://docs.fluentvalidation.net/en/latest/)

### FluentAssertions

[FluentAssertions](https://github.com/fluentassertions/fluentassertions)

### Noda Time

[Noda Time](https://nodatime.org/)

### Humanizer

[Humanizer](https://github.com/Humanizr/Humanizer)

### MediatR

[MediatR](https://github.com/jbogard/MediatR)

### AppMetrics

[AppMetrics](https://github.com/AppMetrics/AppMetrics)

### BenchmarkDotNet

[BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet)

## 사용한 Nuget 패키지

[AspNetCore.HealthChecks.ApplicationStatus](https://www.nuget.org/packages/AspNetCore.HealthChecks.ApplicationStatus)

[AspNetCore.HealthChecks.UI](https://www.nuget.org/packages/AspNetCore.HealthChecks.UI)

[AspNetCore.HealthChecks.UI.Client](https://www.nuget.org/packages/AspNetCore.HealthChecks.UI.Client)

[AspNetCore.HealthChecks.UI.InMemory.Storage](https://www.nuget.org/packages/AspNetCore.HealthChecks.UI.InMemory.Storage)

[Microsoft.AspNetCore.OpenApi](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/7.0.9)

[Serilog.AspNetCore](https://www.nuget.org/packages/Serilog.AspNetCore)

[Serilog.Settings.Configuration](https://www.nuget.org/packages/Serilog.Settings.Configuration)

[Serilog.Sinks.Async](https://www.nuget.org/packages/Serilog.Sinks.Async)

[Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore)

[Swashbuckle.AspNetCore.Annotations](https://www.nuget.org/packages/Swashbuckle.AspNetCore.Annotations)

[Quartz](https://www.nuget.org/packages/Quartz)

[Ulid](https://www.nuget.org/packages/Ulid)

## MySQL

[SQL](./Account.sql)
