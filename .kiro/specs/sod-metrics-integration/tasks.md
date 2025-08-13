# 구현 계획

- [x] 1. ITelemetryService 인터페이스에 RTT 메트릭 메서드 추가
  - ITelemetryService.cs에 RecordRttMetrics 메서드 시그니처 추가
  - XML 문서 주석으로 메서드 설명 작성 (한국어)
  - 매개변수 유효성 검사 요구사항 명시
  - _요구사항: 1.1, 4.2_

- [x] 2. TelemetryService 구현체에 RTT 메트릭 필드 및 초기화 코드 추가
  - RTT 관련 메트릭 필드 선언 (Counter, Histogram, Gauge)
  - 생성자에서 RTT 메트릭 인스턴스 초기화
  - OpenTelemetry 명명 규칙에 따른 메트릭 이름 설정
  - _요구사항: 1.1, 4.1, 4.2_

- [x] 3. TelemetryService에 RecordRttMetrics 메서드 구현
  - 입력 매개변수 유효성 검사 로직 구현
  - TagList를 사용한 메트릭 태그 생성
  - 각 메트릭 타입별 기록 로직 구현 (Counter, Histogram, Gauge)
  - 예외 처리 및 로깅 추가
  - _요구사항: 1.1, 1.2, 4.3, 5.1_

- [x] 4. GamePulse 프로젝트에 Demo.Application 참조 추가
  - GamePulse.csproj에 Demo.Application 프로젝트 참조 추가
  - 네임스페이스 충돌 방지를 위한 using 문 정리
  - 순환 참조 검사 및 해결
  - _요구사항: 2.1_

- [x] 5. RttCommand에서 SodMetrics를 ITelemetryService로 교체
  - RttCommand.cs에서 SodMetrics 의존성 제거
  - ITelemetryService 의존성 주입으로 변경
  - AddRtt 메서드 호출을 RecordRttMetrics 호출로 변경
  - 매개변수 매핑 및 단위 변환 검증
  - _요구사항: 2.2, 3.1_

- [x] 6. GamePulse 의존성 주입 설정 수정
  - SodInitialize.cs에서 SodMetrics 등록 제거
  - ITelemetryService 및 TelemetryService 등록 추가
  - 서비스 생명주기 설정 (Singleton)
  - OtelConfig를 TelemetryService 생성자에 매핑
  - _요구사항: 2.1, 3.1_

- [x] 7. OpenTelemetry 설정에서 SodMetrics 참조 제거
  - OpenTelemetryInitialize.cs에서 SodMetrics.M1 참조 제거
  - TelemetryService의 MeterName을 OpenTelemetry에 등록
  - 메트릭 수집 설정 검증
  - _요구사항: 2.1, 4.1_

- [x] 8. SodMetrics.cs 파일 삭제
  - GamePulse/Sod/Metrics/SodMetrics.cs 파일 제거
  - 컴파일 오류 확인 및 해결
  - 사용하지 않는 using 문 정리
  - _요구사항: 2.1, 2.3_

- [-] 9. 단위 테스트 작성
  - TelemetryService.RecordRttMetrics 메서드 단위 테스트 작성
  - 유효한 입력값에 대한 메트릭 기록 검증 테스트
  - 잘못된 입력값에 대한 예외 처리 테스트
  - 메트릭 태그 정확성 검증 테스트
  - _요구사항: 3.2, 5.2_

- [ ] 10. 통합 테스트 및 검증
  - RttCommand 실행 시 ITelemetryService 호출 검증
  - OpenTelemetry 메트릭 수집 동작 확인
  - 기존 텔레메트리 기능 회귀 테스트
  - 전체 애플리케이션 빌드 및 실행 테스트
  - _요구사항: 2.1, 3.1, 3.3_