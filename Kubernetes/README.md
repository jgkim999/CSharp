# Kubernetes

Windows 환경

## wsl

powershell실행, wsl업데이트

```bash
wsl --update
```

powershell실행, 설치 가능한 linux확인

```bash
wsl --list --online

Ubuntu                                 Ubuntu
Debian                                 Debian GNU/Linux
kali-linux                             Kali Linux Rolling
Ubuntu-18.04                           Ubuntu 18.04 LTS
Ubuntu-20.04                           Ubuntu 20.04 LTS
Ubuntu-22.04                           Ubuntu 22.04 LTS
OracleLinux_7_9                        Oracle Linux 7.9
OracleLinux_8_7                        Oracle Linux 8.7
OracleLinux_9_1                        Oracle Linux 9.1
openSUSE-Leap-15.5                     openSUSE Leap 15.5
SUSE-Linux-Enterprise-Server-15-SP4    SUSE Linux Enterprise Server 15 SP4
SUSE-Linux-Enterprise-15-SP5           SUSE Linux Enterprise 15 SP5
openSUSE-Tumbleweed                    openSUSE Tumbleweed
```

설치

```bash
wsl --install -d Ubuntu-22.04
```

powershell실행 wsl 설치된 linux 확인

```bash
wsl --list

Linux용 Windows 하위 시스템 배포:
Ubuntu-22.04

Amazon2
docker-desktop
Ubuntu-20.04 (기본값)
docker-desktop-data
```

기본 linux 지정

```bash
wsl --setdefault Ubuntu-22.04

Linux용 Windows 하위 시스템 배포:
Ubuntu-22.04 (기본값)

Amazon2
docker-desktop
Ubuntu-20.04
docker-desktop-data
```

## shell

ubuntu 22.04로 진행, shell확인

```bash
echo "$SHELL"

/bin/bash
```

필요 패키지 설치

```bash
Ubuntu
sudo apt install wget curl git unzip

AWS EC2
yum install wget curl git unzip
```

zsh설치

```bash
Ubuntu
sudo apt install zsh

AWS EC2
yum install zsh
zsh --version
```

기본 shell 변경

```bash
Ubuntu
chsh -s $(which zsh)

AWS EC2
yum install util-linux-user.x86_64
chsh -s /bin/zsh
```

재접속, 2번 선택

oh-my-zsh설치

```bash
Ubuntu
curl
sh -c "$(curl -fsSL https://raw.githubusercontent.com/robbyrussell/oh-my-zsh/master/tools/install.sh)"

AWS EC2
curl -L https://raw.github.com/robbyrussell/oh-my-zsh/master/tools/install.sh | sh
```

테마 변경

```bash
vi ~/.zshrc

ZSH_THEME="agnoster"
```

재접속, 폰트 설치

```bash
Ubuntu
sudo apt install fonts-powerline​

AWS EC2
# clone
git clone https://github.com/powerline/fonts.git --depth=1
# install
cd fonts
./install.sh
# clean-up a bit
cd ..
rm -rf fonts
```

플러그인 다운로드

```bash
# zsh-syntax-highlighting
git clone https://github.com/zsh-users/zsh-syntax-highlighting.git ${ZSH_CUSTOM:-~/.oh-my-zsh/custom}/plugins/zsh-syntax-highlighting

# zsh-autosuggestions
git clone https://github.com/zsh-users/zsh-autosuggestions ${ZSH_CUSTOM:-~/.oh-my-zsh/custom}/plugins/zsh-autosuggestions

# fzf (Fuzzy Finder )
git clone --depth 1 https://github.com/junegunn/fzf.git ~/.fzf

~/.fzf/install​
```

플러그인 사용설정

```bash
vi ~/.zshrc

plugins=(
git
sudo
colored-man-pages
zsh-syntax-highlighting
zsh-autosuggestions
fzf
)​
```

설정 적용

```bash
source ~/.zshrc​
```

```bash
Ubuntu
sudo apt install fontconfig
sudo apt install unzip
wget https://github.com/naver/d2codingfont/releases/download/VER1.3.2/D2Coding-Ver1.3.2-20180524.zip
sudo unzip -d /usr/share/fonts/d2coding D2Coding-Ver1.3.2-20180524.zip
sudo fc-cache -f -v

AWS EC2
sudo yum install fontconfig
sudo yum install unzip
wget https://github.com/naver/d2codingfont/releases/download/VER1.3.2/D2Coding-Ver1.3.2-20180524.zip
unzip -d /usr/share/fonts/d2coding D2Coding-Ver1.3.2-20180524.zip
fc-cache -f -v
```
