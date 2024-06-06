# .NET Practice Project

```bash
# dev namespace 생성
> kubectl create -f .\dev_namespace.yaml

# dev namespace 사용률 지정
> kubectl apply -f .\quota-mem-cpu.yaml --namespace=dev
# 지우려면
> kubectl delete -f .\quota-mem-cpu.yaml

# dev namespace 사용률 보기
> kubectl get resourcequota mem-cpu-dev --namespace=dev --output=yaml

# namespace dev선택하기
> kubectl config set-context --current --namespace=dev

# 확인하기
> kubectl config view --minify

# nginx 배포해보기
> kubectl apply -f .\nginx.yaml

# dashboard를 위한 admin-user만들기
> kubectl apply -f .\dashboard-adminuser.yaml

# dashboard role binding
> kubectl apply -f .\dashboard-adminuser-rolebinding.yaml

# token
> kubectl -n kubernetes-dashboard create token admin-user

eyJhbGciOiJSUzI1NiIsImtpZCI6Ik51bTZXMkRkVWE4OFZuWnlTN3A0RXB6TldLLU9wWmpoUFEwa1lPUzQ2TmcifQ.eyJhdWQiOlsiaHR0cHM6Ly9rdWJlcm5ldGVzLmRlZmF1bHQuc3ZjLmNsdXN0ZXIubG9jYWwiXSwiZXhwIjoxNzE2NjA0NTEyLCJpYXQiOjE3MTY2MDA5MTIsImlzcyI6Imh0dHBzOi8va3ViZXJuZXRlcy5kZWZhdWx0LnN2Yy5jbHVzdGVyLmxvY2FsIiwianRpIjoiNjVhOTgxN2YtM2Q3MC00Y2MzLWI4NDQtZjlkNmY0MjRjZjkwIiwia3ViZXJuZXRlcy5pbyI6eyJuYW1lc3BhY2UiOiJrdWJlcm5ldGVzLWRhc2hib2FyZCIsInNlcnZpY2VhY2NvdW50Ijp7Im5hbWUiOiJhZG1pbi11c2VyIiwidWlkIjoiMWE1MDMzNDUtZjRkMy00NzdkLTgzZTYtMWU0MGJmNTQ1YWNiIn19LCJuYmYiOjE3MTY2MDA5MTIsInN1YiI6InN5c3RlbTpzZXJ2aWNlYWNjb3VudDprdWJlcm5ldGVzLWRhc2hib2FyZDphZG1pbi11c2VyIn0.N_1WNSg7uQ5aKqehNVpxdQsNhZQf0C9skjkK1NTUn4Y8AojATbwqyiX6TYSnVmjswBnF-zgHRyvSxNfjcmM3LVwv7rLtj-8asWnVuYAL_1FPZoXBEGxZasH9BjzxvLdVLChWbD6-WJ7qz159Yx2OvySJ5g_gwg1SuH6PerN4p06ZOG81yV_Uo5mP19Y4VqgHPk47fera6eZXUAZbXzBS_OSqmTj_5ibQyu3bHScsq8serTBerxzWzws4_3xou-S72IFlSfgteqBFAfFuaCi62LSP49IVpcalUTzzXzxHoRWeXRua98nRPDPld6aj9qBCZHc-QP34duISUtsGKQVlWQ

> kubectl apply -f .\dashboard-adminuser-long-live-token.yaml
secret/admin-user created

# dashboard 배포
> kubectl apply -f https://raw.githubusercontent.com/kubernetes/dashboard/v2.6.1/aio/deploy/recommended.yaml

# dashboard 활성화
> kubectl proxy

# dashboard 접근
http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/
```
