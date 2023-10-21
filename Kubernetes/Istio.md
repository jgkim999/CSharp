# Istio

- [Istio](#istio)
  - [설치](#설치)
  - [참고](#참고)

## 설치

```bash
curl -L https://istio.io/downloadIstio | sh -
cd istio-1.19.0 && export PATH=$PWD/bin:$PATH
```

```bash
istioctl install --set profile=demo -y
```

## 참고

https://domdom.tistory.com/598
