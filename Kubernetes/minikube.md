# Minikube

- [Minikube](#minikube)
  - [Installation](#installation)
  - [참고](#참고)

## Installation

```bash
curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
sudo install minikube-linux-amd64 /usr/local/bin/minikube
```

```bash
minikube start
```

```bash
kubectl get po -A
```

최신버전 체크

```bash
minikube kubectl -- get po -A
```

명령어 매핑

```bash
alias kubectl="minikube kubectl --"
```

Dashboard

```bash
minikube dashboard
```


## 참고

https://minikube.sigs.k8s.io/docs/start/