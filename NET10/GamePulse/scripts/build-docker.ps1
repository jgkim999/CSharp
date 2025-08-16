# GamePulse Docker 빌드 스크립트 (PowerShell)
# 요구사항 1.1, 1.2를 충족하는 최적화된 Docker 이미지 빌드

param(
    [string]$ImageName = $env:IMAGE_NAME ?? "gamepulse",
    [string]$ImageTag = $env:IMAGE_TAG ?? "latest",
    [string]$BuildContext = $env:BUILD_CONTEXT ?? (Split-Path -Parent $PSScriptRoot),
    [string]$DockerfilePath = $env:DOCKERFILE_PATH ?? (Join-Path (Split-Path -Parent $PSScriptRoot) "Dockerfile"),
    [string]$Platform = $env:PLATFORM,
    [string]$DockerRegistry = $env:DOCKER_REGISTRY,
    [switch]$NoCache,
    [switch]$Push,
    [switch]$Scan,
    [switch]$MultiArch,
    [switch]$Help
)

# ============================================================================
# 유틸리티 함수
# ============================================================================

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )

    $colorMap = @{
        "Red" = [ConsoleColor]::Red
        "Green" = [ConsoleColor]::Green
        "Yellow" = [ConsoleColor]::Yellow
        "Blue" = [ConsoleColor]::Blue
        "White" = [ConsoleColor]::White
    }

    Write-Host $Message -ForegroundColor $colorMap[$Color]
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "[INFO] $Message" "Blue"
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "[SUCCESS] $Message" "Green"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "[WARNING] $Message" "Yellow"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "[ERROR] $Message" "Red"
}

function Show-Help {
    @"
GamePulse Docker 빌드 스크립트 (PowerShell)

사용법:
    .\build-docker.ps1 [매개변수]

매개변수:
    -ImageName NAME         이미지 이름 (기본값: gamepulse)
    -ImageTag TAG          이미지 태그 (기본값: latest)
    -BuildContext PATH     빌드 컨텍스트 경로 (기본값: 현재 프로젝트 루트)
    -DockerfilePath PATH   Dockerfile 경로 (기본값: ./Dockerfile)
    -Platform PLATFORM     대상 플랫폼 (예: linux/amd64)
    -DockerRegistry URL    Docker 레지스트리 URL
    -NoCache               캐시 없이 빌드
    -Push                  빌드 후 레지스트리에 푸시
    -Scan                  빌드 후 보안 스캔 실행
    -MultiArch             멀티 아키텍처 빌드
    -Help                  이 도움말 출력

환경 변수:
    IMAGE_NAME             이미지 이름
    IMAGE_TAG              이미지 태그
    BUILD_CONTEXT          빌드 컨텍스트 경로
    DOCKERFILE_PATH        Dockerfile 경로
    DOCKER_REGISTRY        Docker 레지스트리 URL
    PLATFORM               대상 플랫폼

예제:
    # 기본 빌드
    .\build-docker.ps1

    # 특정 태그로 빌드
    .\build-docker.ps1 -ImageTag "v1.0.0"

    # 캐시 없이 빌드
    .\build-docker.ps1 -NoCache

    # 빌드 후 푸시
    .\build-docker.ps1 -ImageTag "v1.0.0" -Push
"@
}

function Test-Docker {
    try {
        $dockerVersion = docker --version
        if ($LASTEXITCODE -ne 0) {
            throw "Docker 명령어 실행 실패"
        }

        docker info | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Docker 데몬이 실행되지 않음"
        }

        Write-Info "Docker 버전: $dockerVersion"
        return $true
    }
    catch {
        Write-Error "Docker가 설치되지 않았거나 실행되지 않았습니다: $_"
        return $false
    }
}

function Enable-BuildKit {
    if (-not $env:DOCKER_BUILDKIT -or $env:DOCKER_BUILDKIT -ne "1") {
        Write-Info "Docker BuildKit 활성화 중..."
        $env:DOCKER_BUILDKIT = "1"
    }
}

function Test-BuildContext {
    param(
        [string]$Context,
        [string]$DockerfilePath
    )

    if (-not (Test-Path $Context)) {
        Write-Error "빌드 컨텍스트 디렉토리가 존재하지 않습니다: $Context"
        return $false
    }

    if (-not (Test-Path $DockerfilePath)) {
        Write-Error "Dockerfile이 존재하지 않습니다: $DockerfilePath"
        return $false
    }

    Write-Info "빌드 컨텍스트: $Context"
    Write-Info "Dockerfile: $DockerfilePath"
    return $true
}

function Get-ImageSize {
    param([string]$ImageName)

    try {
        $imageInfo = docker images --format "{{.Size}}" $ImageName | Select-Object -First 1
        Write-Info "빌드된 이미지 크기: $imageInfo"

        # 이미지 크기 경고 (간단한 체크)
        if ($imageInfo -match "(\d+\.?\d*)\s*GB" -and [double]$matches[1] -gt 0.5) {
            Write-Warning "이미지 크기가 500MB를 초과합니다. 최적화를 고려해보세요."
        }
    }
    catch {
        Write-Warning "이미지 크기를 확인할 수 없습니다: $_"
    }
}

function Invoke-SecurityScan {
    param([string]$ImageName)

    Write-Info "보안 스캔 실행 중..."

    # Docker Scout 확인
    try {
        docker scout version | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Info "Docker Scout로 스캔 중..."
            docker scout cves $ImageName
        }
    }
    catch {
        Write-Warning "Docker Scout를 사용할 수 없습니다."
    }

    # Trivy 확인
    try {
        trivy --version | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Info "Trivy로 스캔 중..."
            trivy image $ImageName
        }
    }
    catch {
        Write-Warning "Trivy를 사용할 수 없습니다."
    }
}

function Build-MultiArchImage {
    param(
        [string]$ImageFullName,
        [string]$Context,
        [string]$DockerfilePath,
        [string]$Platform = "linux/amd64,linux/arm64",
        [bool]$NoCache,
        [bool]$Push
    )

    Write-Info "멀티 아키텍처 빌드 시작 (플랫폼: $Platform)"

    # buildx 빌더 확인 및 생성
    $builderExists = docker buildx ls | Select-String "gamepulse-builder"
    if (-not $builderExists) {
        Write-Info "buildx 빌더 생성 중..."
        docker buildx create --name gamepulse-builder --use
    }
    else {
        docker buildx use gamepulse-builder
    }

    # 빌드 명령어 구성
    $buildArgs = @(
        "buildx", "build",
        "--platform", $Platform,
        "--file", $DockerfilePath,
        "--tag", $ImageFullName
    )

    if ($NoCache) {
        $buildArgs += "--no-cache"
    }

    if ($Push) {
        $buildArgs += "--push"
    }
    else {
        $buildArgs += "--load"
    }

    $buildArgs += $Context

    Write-Info "빌드 명령어: docker $($buildArgs -join ' ')"
    & docker @buildArgs

    if ($LASTEXITCODE -ne 0) {
        throw "멀티 아키텍처 빌드 실패"
    }
}

function Build-SingleArchImage {
    param(
        [string]$ImageFullName,
        [string]$Context,
        [string]$DockerfilePath,
        [string]$Platform,
        [bool]$NoCache
    )

    Write-Info "단일 아키텍처 빌드 시작"

    # 빌드 명령어 구성
    $buildArgs = @(
        "build",
        "--file", $DockerfilePath,
        "--tag", $ImageFullName
    )

    if ($NoCache) {
        $buildArgs += "--no-cache"
    }

    if ($Platform) {
        $buildArgs += "--platform", $Platform
    }

    # 빌드 인자 추가
    $buildDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    $buildArgs += "--build-arg", "BUILDKIT_INLINE_CACHE=1"
    $buildArgs += "--build-arg", "BUILD_DATE=$buildDate"

    # Git 커밋 해시 (가능한 경우)
    try {
        $gitHash = git rev-parse --short HEAD 2>$null
        if ($gitHash) {
            $buildArgs += "--build-arg", "VCS_REF=$gitHash"
        }
    }
    catch {
        $buildArgs += "--build-arg", "VCS_REF=unknown"
    }

    $buildArgs += $Context

    Write-Info "빌드 명령어: docker $($buildArgs -join ' ')"
    & docker @buildArgs

    if ($LASTEXITCODE -ne 0) {
        throw "빌드 실패"
    }
}

function Push-Image {
    param([string]$ImageFullName)

    Write-Info "이미지 푸시 중: $ImageFullName"
    docker push $ImageFullName

    if ($LASTEXITCODE -ne 0) {
        throw "이미지 푸시 실패"
    }

    Write-Success "이미지 푸시 완료"
}

# ============================================================================
# 메인 함수
# ============================================================================

function Main {
    if ($Help) {
        Show-Help
        return
    }

    # 전체 이미지 이름 구성
    $imageFullName = "$ImageName`:$ImageTag"
    if ($DockerRegistry) {
        $imageFullName = "$DockerRegistry/$imageFullName"
    }

    Write-Info "GamePulse Docker 빌드 시작"
    Write-Info "이미지: $imageFullName"

    try {
        # 사전 검사
        if (-not (Test-Docker)) {
            exit 1
        }

        Enable-BuildKit

        if (-not (Test-BuildContext -Context $BuildContext -DockerfilePath $DockerfilePath)) {
            exit 1
        }

        # 빌드 시작 시간 기록
        $startTime = Get-Date

        # 빌드 실행
        if ($MultiArch) {
            $platformList = if ($Platform) { $Platform } else { "linux/amd64,linux/arm64" }
            Build-MultiArchImage -ImageFullName $imageFullName -Context $BuildContext -DockerfilePath $DockerfilePath -Platform $platformList -NoCache $NoCache -Push $Push
        }
        else {
            Build-SingleArchImage -ImageFullName $imageFullName -Context $BuildContext -DockerfilePath $DockerfilePath -Platform $Platform -NoCache $NoCache

            # 이미지 크기 확인
            Get-ImageSize -ImageName $imageFullName

            # 이미지 푸시
            if ($Push) {
                Push-Image -ImageFullName $imageFullName
            }

            # 보안 스캔 실행
            if ($Scan) {
                Invoke-SecurityScan -ImageName $imageFullName
            }
        }

        # 빌드 완료 시간 계산
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds

        Write-Success "빌드 완료! (소요 시간: $([math]::Round($duration, 2))초)"
        Write-Info "이미지: $imageFullName"

        # 빌드 결과 요약
        Write-Host ""
        Write-Host "=== 빌드 결과 요약 ===" -ForegroundColor Cyan
        Write-Host "이미지 이름: $imageFullName"
        Write-Host "빌드 시간: $([math]::Round($duration, 2))초"
        if (-not $MultiArch) {
            try {
                $imageSize = docker images --format "{{.Size}}" $imageFullName | Select-Object -First 1
                Write-Host "이미지 크기: $imageSize"
            }
            catch {
                Write-Host "이미지 크기: 확인 불가"
            }
        }
        Write-Host "빌드 컨텍스트: $BuildContext"
        Write-Host "Dockerfile: $DockerfilePath"
    }
    catch {
        Write-Error "빌드 실패: $_"
        exit 1
    }
}

# 스크립트 실행
Main
