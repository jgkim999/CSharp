# Git 규칙

## Git Branch 네이밍 규칙

브랜치 이름은 다음과 같은 형식을 따릅니다:

```text
<type>/<issue-number>-<description>
```

- `type`: 브랜치의 유형을 나타냅니다. 예: `feature`, `bugfix`, `hotfix`, `release`
- `issue-number`: 관련된 이슈 번호를 포함합니다.
- `description`: 간략한 설명을 포함합니다.
- 항상 소문자를 사용하고 단어 사이는 `-` 를 이용합니다.

브랜치 이름은 명확하고 일관되게 작성되어야 하며, 팀원들이 브랜치의 목적을 쉽게 이해할 수 있도록 해야 합니다.
브랜치 이름은 다음과 같은 형식을 따릅니다:

| `type` | 설명 |
|--|--|
| `feature` | 새로운 기능 개발을 위한 브랜치 |
| `bugfix` | 버그 수정을 위한 브랜치 |
| `hotfix` | 긴급 버그 수정을 위한 브랜치 |
| `release` | 배포를 위한 브랜치 |

예시:

```text
feature/123-add-login-functionality
bugfix/456-fix-login-error
hotfix/789-fix-critical-bug
release/1.0.0
```

## Git Commit 메시지 작성 규칙

커밋 메시지는 다음과 같은 형식을 따릅니다:

```text
<type>(<scope>): <subject>
<body>
<footer>
```

- `type`: 커밋의 유형을 나타냅니다. 예: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`
- `scope`: 변경 사항의 범위를 나타냅니다. 선택 사항입니다.
- `subject`: 변경 사항에 대한 간략한 설명. 50자 이내로 작성합니다.
- `body`: 변경 사항에 대한 자세한 설명. 선택 사항입니다.
- `footer`: 이슈 트래킹 번호나 관련된 PR 링크 등을 포함합니다. 선택 사항입니다.

예시:

```text
feat: add login functionality
Implement user login with JWT authentication.
fix(auth): resolve login error
Fixes #123
```

## Gitmoji 사용 규칙

Gitmoji를 사용하여 커밋 메시지를 작성합니다. Gitmoji는 커밋 메시지를 시각적으로 표현할 수 있는 이모지입니다. 이를 통해 커밋의 목적을 쉽게 이해할 수 있습니다.

Gitmoji는 다음과 같은 형식을 따릅니다:

```text
<gitmoji> <type>(<scope>): <subject>
```

Gitmoji는 커밋 메시지의 시작 부분에 위치하며, 커밋의 유형을 나타냅니다. 예를 들어, `:bug:` 는 버그 수정, `:sparkles:` 는 새로운 기능 추가를 의미합니다.

Gitmoji는 커밋 메시지의 가독성을 높이고, 팀원들이 커밋의 목적을 쉽게 이해할 수 있도록 도와줍니다.

Gitmoji를 사용하여 커밋 메시지를 작성할 때는 다음과 같은 규칙을 따릅니다:

- 커밋 메시지의 첫 번째 줄에 Gitmoji를 포함합니다.
- 커밋 메시지의 첫 번째 줄은 50자 이내로 작성합니다.
- 커밋 메시지의 본문은 72자 이내로 작성합니다.
- 커밋 메시지의 본문은 변경 사항에 대한 자세한 설명을 포함합니다.
- 커밋 메시지의 본문은 선택 사항입니다.
- 커밋 메시지의 본문은 여러 줄로 작성할 수 있으며, 각 줄은 72자 이내로 작성합니다.

Gitmoji는 다음과 같은 이모지를 사용합니다. 자세한 내용은 [Gitmoji 공식 사이트](https://gitmoji.dev/)를 참고하세요.

| 아이콘 | 코드 | 설명 | 원문 |
|------|------|------|------|
| 👽 | `:alien:` | 외부 API 변화로 인한 수정 | Update code due to external API changes. |
| 🚑 | `:ambulance:` | 긴급 수정 | Critical hotfix. |
| 🎨 | `:art:` | 코드의 구조/형태 개선 | Improve structure / format of the code. |
| 🍻 | `:beers:` | 술 취해서 쓴 코드 | Write code drunkenly. |
| 🔖 | `:bookmark:` | 릴리즈/버전 태그 | Release / Version tags. |
| 🐛 | `:bug:` | 버그 수정 | Fix a bug. |
| 💡 | `:bulb:` | 주석 추가/수정 | Add or update comments in source code. |
| 🗃 | `:card_file_box:` | 데이터베이스 관련 수정 | Perform database related changes. |
| 📈 | `:chart_with_upwards_trend:` | 분석, 추적 코드 추가/수정 | Add or update analytics or track code. |
| 👷 | `:construction_worker:` | CI 빌드 시스템 추가/수정 | Add or update CI build system. |
| 🚧 | `:construction:` | 작업 중인 코드 | Work in progress. |
| 🔥 | `:fire:` | 코드/파일 삭제 | Remove code or files. |
| 🌐 | `:globe_with_meridians:` | 국제화/현지화 | Internationalization and localization. |
| 💚 | `:green_heart:` | CI 빌드 수정 | Fix CI Build. |
| 🔨 | `:hammer:` | 개발 스크립트 추가/수정 | Add or update development scripts. |
| 🛠 | `:hammer_and_wrench:` | 도구 관련 수정 | Add or update tools. |
| ➖ | `:heavy_minus_sign:` | 의존성 제거 | Remove a dependency. |
| ➕ | `:heavy_plus_sign:` | 의존성 추가 | Add a dependency. |
| 💄 | `:lipstick:` | UI/스타일 파일 추가/수정 | Add or update the UI and style files. |
| 🔒 | `:lock:` | 보안 이슈 수정 | Fix security issues. |
| 🔊 | `:loud_sound:` | 로그 추가/수정 | Add or update logs. |
| 📝 | `:memo:` | 문서 추가/수정 | Add or update documentation. |
| 📦 | `:package:` | 컴파일된 파일 추가/수정 | Add or update compiled files or packages. |
| 📄 | `:page_facing_up:` | 라이센스 추가/수정 | Add or update license. |
| 💩 | `:poop:` | 똥싼 코드 | Write bad code that needs to be improved. |
| 📌 | `:pushpin:` | 특정 버전 의존성 고정 | Pin dependencies to specific versions. |
| ♻️ | `:recycle:` | 코드 리팩토링 | Refactor code. |
| ⏪ | `:rewind:` | 변경 내용 되돌리기 | Revert changes. |
| 🙈 | `:see_no_evil:` | .gitignore 추가/수정 | Add or update a .gitignore file. |
| ✨ | `:sparkles:` | 새 기능 | Introduce new features. |
| 🎉 | `:tada:` | 프로젝트 시작 | Begin a project. |
| 🚚 | `:truck:` | 리소스 이동, 이름 변경 | Move or rename resources (e.g.: files paths routes). |
| 🔀 | `:twisted_rightwards_arrows:` | 브랜치 합병 | Merge branches. |
| ✅ | `:white_check_mark:` | 테스트 추가/수정 | Add or update tests. |
| 🔧 | `:wrench:` | 구성 파일 추가/삭제 | Add or update configuration files. |
| ⚡️ | `:zap:` | 성능 개선 | Improve performance. |
