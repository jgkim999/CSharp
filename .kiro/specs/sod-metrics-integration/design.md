# 설계 문서

## 개요

이 설계는 GamePulse 프로젝트의 SodMetrics.cs에 있는 RTT(Round Trip Time) 메트릭 기능을 Demo.Application 프로젝트의 ITelemetryService 인터페이스에 통합하는 방법을 정의합니다. 이를 통해 중복된 메트릭 서비스를 제거하고 통합된 텔레메트리 아키텍처를 구축합니다.

## 아키텍처

### 현재 상태

```text
GamePulse 프로젝트:
├── SodMetrics.cs (제거 대상)
│   ├── RTT Counter
│   ├── RTT Histogram  
│   ├── Network Quality Histogram
│   └── RTT Gauge
└── RttCommand.cs (SodMetrics 의존성)

Demo.Application 프로젝트:
├── ITelemetryService.cs (확장 대상)
└── TelemetryService.cs (구현 확장)
```

### 목표 상태

```text
Demo.Application 프로젝트:
├── ITelemetryService.cs (RTT 메서드 추가)
│   └── RecordRttMetrics() 메서드 추가
└── TelemetryService.cs (RTT 메트릭 구현)
    ├── 기존 메트릭들
    └── RTT 관련 메트릭들
        ├── RTT Counter
        ├── RTT Histogram
        ├── Network Quality Histogram
        └── RTT Gauge

GamePulse 프로젝트:
└── RttCommand.cs (ITelemetryService 사용)
```

## 컴포넌트 및 인터페이스

### 1. ITelemetryService 인터페이스 확장

새로운 메서드 추가:

```csharp
/// <summary>
/// RTT(Round Trip Time) 메트릭을 기록합니다.
/// </summary>
/// <param name="countryCode">국가 코드</param>
/// <param name="rtt">RTT 값 (초 단위)</param>
/// <param name="quality">네트워크 품질 점수</param>
/// <param name="gameType">게임 타입 (기본값: "sod")</param>
void RecordRttMetrics(string countryCode, double rtt, double quality, string gameType = "sod");
```

### 2. TelemetryService 구현체 확장

새로운 메트릭 필드 추가:

- `Counter<long> _rttCounter`: RTT 호출 횟수
- `Histogram<double> _rttHistogram`: RTT 지속 시간 분포
- `Histogram<double> _networkQualityHistogram`: 네트워크 품질 분포  
- `Gauge<double> _rttGauge`: 현재 RTT 값

### 3. 의존성 주입 구성

GamePulse 프로젝트에서:

- SodMetrics 등록 제거
- ITelemetryService 등록 추가
- OpenTelemetry 설정에서 SodMetrics.M1 제거

## 데이터 모델

### RTT 메트릭 태그 구조

```csharp
TagList rttTags = new TagList
{
    { "country", countryCode },
    { "game", gameType }
};
```

### 메트릭 명명 규칙

- RTT Counter: `"demo_app_rtt_calls_total"`
- RTT Histogram: `"demo_app_rtt_duration_seconds"`
- Network Quality: `"demo_app_network_quality_score"`
- RTT Gauge: `"demo_app_rtt_current_seconds"`

## 에러 처리

### 1. 입력 유효성 검사

- `countryCode`: null 또는 빈 문자열 검사
- `rtt`: 음수 값 검사
- `quality`: 유효 범위 검사 (0-100 가정)

### 2. 메트릭 기록 실패 처리

- 메트릭 기록 중 예외 발생 시 로깅
- 애플리케이션 흐름에 영향을 주지 않도록 예외 격리

### 3. 리소스 관리

- Meter 및 관련 메트릭 인스턴스의 적절한 Dispose 처리
- 메모리 누수 방지를 위한 리소스 정리

## 테스트 전략

### 1. 단위 테스트

- ITelemetryService.RecordRttMetrics 메서드 테스트
- 다양한 입력 값에 대한 메트릭 기록 검증
- 예외 상황 처리 테스트

### 2. 통합 테스트

- RttCommand에서 ITelemetryService 사용 테스트
- OpenTelemetry 메트릭 수집 검증
- 기존 기능 회귀 테스트

### 3. 성능 테스트

- 메트릭 기록 성능 측정
- 메모리 사용량 모니터링
- 동시성 테스트

## 마이그레이션 계획

### 1단계: ITelemetryService 확장

- 인터페이스에 RecordRttMetrics 메서드 추가
- TelemetryService에 RTT 메트릭 구현 추가

### 2단계: GamePulse 프로젝트 수정

- RttCommand에서 SodMetrics 대신 ITelemetryService 사용
- 의존성 주입 설정 변경

### 3단계: SodMetrics 제거

- SodMetrics.cs 파일 삭제
- OpenTelemetry 설정에서 SodMetrics 참조 제거
- 빌드 및 테스트 검증

### 4단계: 정리 및 검증

- 사용하지 않는 using 문 정리
- 전체 시스템 테스트 수행
- 문서 업데이트

## 호환성 고려사항

### 1. OpenTelemetry 버전 호환성

- 두 프로젝트 간 OpenTelemetry 패키지 버전 일치 확인
- 메트릭 API 호환성 검증

### 2. 네임스페이스 및 어셈블리 참조

- Demo.Application 프로젝트를 GamePulse에서 참조 가능하도록 설정
- 순환 참조 방지

### 3. 설정 관리

- OtelConfig와 TelemetryService 초기화 매개변수 매핑
- 서비스 이름 및 버전 일관성 유지

## 보안 고려사항

### 1. 민감한 정보 보호

- IP 주소 정보의 적절한 처리
- 로그에서 개인정보 마스킹

### 2. 메트릭 데이터 보안

- 메트릭 수집 엔드포인트 보안
- 인증 및 권한 부여 고려

## 성능 최적화

### 1. 메트릭 수집 최적화

- 배치 처리를 통한 성능 향상
- 메트릭 샘플링 전략 적용

### 2. 메모리 사용량 최적화

- 태그 재사용을 통한 메모리 절약
- 불필요한 객체 생성 최소화

## 모니터링 및 관찰성

### 1. 메트릭 대시보드

- RTT 메트릭 시각화
- 국가별 네트워크 품질 모니터링

### 2. 알림 설정

- RTT 임계값 초과 시 알림
- 메트릭 수집 실패 모니터링
