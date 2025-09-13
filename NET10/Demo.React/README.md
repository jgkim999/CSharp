# Demo.React

Demo.Admin Blazor 프로젝트를 참고하여 만든 React 기반의 관리자 인터페이스입니다.

## 기술 스택

- **Next.js 15**: React 프레임워크 (App Router 사용)
- **TypeScript**: 타입 안정성 제공
- **Tailwind CSS**: 유틸리티 우선 CSS 프레임워크
- **TanStack Query**: 서버 상태 관리 및 데이터 페칭
- **TanStack Table**: 데이터 테이블 구현
- **Zustand**: 클라이언트 상태 관리
- **Axios**: HTTP 클라이언트
- **React Hot Toast**: 알림 메시지
- **Heroicons**: 아이콘 라이브러리

## 주요 기능

### 📱 반응형 레이아웃
- 모바일 친화적인 디자인
- 다크/라이트 모드 지원
- 사이드바 네비게이션 (토글 가능)

### 👥 사용자 관리
- 사용자 목록 조회 및 검색
- 서버사이드 페이지네이션
- 로컬스토리지를 이용한 검색어/페이지 크기 저장
- 실시간 데이터 페칭 및 캐싱

### 🎨 UI 컴포넌트
- 재사용 가능한 데이터 테이블 컴포넌트
- 정렬, 필터링, 페이지네이션 지원
- 일관된 디자인 시스템

### 🔄 상태 관리
- Theme 상태 (다크/라이트 모드)
- Navigation 상태 (사이드바 토글)
- 로컬스토리지 동기화

## 프로젝트 구조

```
src/
├── app/                    # Next.js App Router 페이지
│   ├── user/              # 사용자 관리 페이지
│   ├── counter/           # 카운터 예제 페이지
│   ├── weather/           # 날씨 예제 페이지
│   ├── servertime/        # 서버 시간 페이지
│   └── providers/         # React Context Providers
├── components/            # React 컴포넌트
│   ├── layout/           # 레이아웃 컴포넌트
│   └── ui/               # UI 컴포넌트
├── hooks/                # 커스텀 훅
├── lib/                  # 유틸리티 및 설정
└── store/                # Zustand 스토어
```

## 시작하기

### 1. 의존성 설치
```bash
npm install
```

### 2. 환경 변수 설정
`.env.local` 파일에서 API 베이스 URL을 설정합니다:
```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5198
```

### 3. 개발 서버 실행
```bash
npm run dev
```

브라우저에서 [http://localhost:3000](http://localhost:3000)을 열어 확인합니다.

### 4. 빌드
```bash
npm run build
npm start
```

## API 연동

이 프로젝트는 Demo.Web의 API를 사용합니다:

- **사용자 목록**: `POST /api/user/list`
  - 검색, 페이지네이션 지원
  - Demo.Admin과 동일한 API 엔드포인트 사용

## Demo.Admin과의 비교

| 기능 | Demo.Admin (Blazor) | Demo.React |
|------|---------------------|------------|
| 프레임워크 | ASP.NET Core Blazor | Next.js (React) |
| UI 라이브러리 | MudBlazor | Tailwind CSS + Headless UI |
| 상태 관리 | Blazor 컴포넌트 상태 | Zustand |
| 데이터 페칭 | RestSharp | TanStack Query + Axios |
| 다크 모드 | MudBlazor 테마 | Tailwind CSS 다크 모드 |
| 로컬 스토리지 | Blazored.LocalStorage | 커스텀 useLocalStorage 훅 |
| 테이블 | MudDataGrid | TanStack Table |

## 개발 가이드

### 새 페이지 추가
1. `src/app/` 폴더에 새 디렉토리 생성
2. `page.tsx` 파일에 React 컴포넌트 작성
3. `MainLayout`으로 감싸기
4. `src/components/layout/NavMenu.tsx`에 네비게이션 링크 추가

### 새 API 추가
1. `src/lib/api.ts`에 타입 정의 및 함수 추가
2. TanStack Query를 사용하여 데이터 페칭
3. 에러 처리 및 로딩 상태 관리

### 스타일링
- Tailwind CSS 클래스 사용
- 다크 모드는 `dark:` 접두사 사용
- 일관된 색상 팔레트 및 스페이싱 유지

## 라이선스

이 프로젝트는 Demo.Admin을 참고하여 만든 학습 목적의 프로젝트입니다.
