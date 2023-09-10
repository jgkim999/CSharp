# k8s
- [k8s](#k8s)
  - [이전으로](#이전으로)
  - [virtual box 에 k8s node설치](#virtual-box-에-k8s-node설치)
  - [Master node](#master-node)
  - [Worker node](#worker-node)
  - [전부 날리고 다시](#전부-날리고-다시)
  - [오류시](#오류시)
  - [다음, 배포 연습](#다음-배포-연습)

## [이전으로](./k8s.md)

## virtual box 에 k8s node설치

SSH 포트 2222 -> node 22 / virtual box 세팅에서 설정

|       |      |    |
|-------|------|----|
| node1 | 2222 | 22 |
| node2 | 2223 | 22 |

ssh -p 2222 jgkim@127.0.0.1

node에 docker, k8s 설치

방화벽 off

```bash
sudo ufw disable

Firewall stopped and disabled on system startup
```

Swap off

```bash
sudo swapoff -a && sudo sed -i '/swap/s/^/#/' /etc/fstab
```

Docker, containerd 설치

```bash
sudo apt-get update

sudo apt-get install -y \
    ca-certificates \
    curl \
    gnupg \
    lsb-release

curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update

sudo apt-get install -y docker-ce docker-ce-cli containerd.io

# 버전 확인
sudo docker version

# 서비스 시작
sudo systemctl enable docker
sudo systemctl start docker

sudo systemctl enable containerd
sudo systemctl start containerd

# cgroup 변경
sudo mkdir -p /etc/docker
cat <<EOF | sudo tee /etc/docker/daemon.json
{
  "exec-opts": ["native.cgroupdriver=systemd"],
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "100m"
  },
  "storage-driver": "overlay2"
}
EOF

# 재시작
sudo systemctl enable docker
sudo systemctl daemon-reload
sudo systemctl restart docker  

# 변경된 cgroup driver 확인
sudo docker info | grep -i cgroup
```

Kubernetes 설치

```bash
cat <<EOF | sudo tee /etc/modules-load.d/k8s.conf
br_netfilter
EOF

cat <<EOF | sudo tee /etc/sysctl.d/k8s.conf
net.bridge.bridge-nf-call-ip6tables = 1
net.bridge.bridge-nf-call-iptables = 1
EOF

sudo sysctl --system
```

```bash
# 필요한 프로그램 미리 설치
sudo apt-get update
sudo apt-get install -y apt-transport-https ca-certificates curl

# 키등록
curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo apt-key add -

cat <<EOF | sudo tee /etc/apt/sources.list.d/kubernetes.list
deb https://apt.kubernetes.io/ kubernetes-xenial main
EOF
```

```bash
sudo apt-get update
sudo apt-get install -y kubelet kubeadm kubectl

# 자동으로 업데이트 되지 않도록 패키지 버전을 고정시킵니다.
sudo apt-mark hold kubelet kubeadm kubectl
```

```bash
unset KUBECONFIG
export KUBECONFIG=/etc/kubernetes/admin.conf
mkdir -p $HOME/.kube
sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config
sudo chown $(id -u):$(id -g) $HOME/.kube/config
```

## Master node

마스터 노드 초기화

```bash
sudo kubeadm init --pod-network-cidr=10.244.0.0/16 --apiserver-advertise-address=[host ip]
```

마지막에 출력되는 토큰은 꼭 기록하자. worker node가 master노드에 등록되려면 필요하다.

```bash
sudo kubeadm init --pod-network-cidr=10.244.0.0/16 --apiserver-advertise-address=192.168.0.47

[init] Using Kubernetes version: v1.28.1
...
[addons] Applied essential addon: CoreDNS
[addons] Applied essential addon: kube-proxy

Your Kubernetes control-plane has initialized successfully!

To start using your cluster, you need to run the following as a regular user:

  mkdir -p $HOME/.kube
  sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config
  sudo chown $(id -u):$(id -g) $HOME/.kube/config

Alternatively, if you are the root user, you can run:

  export KUBECONFIG=/etc/kubernetes/admin.conf

You should now deploy a pod network to the cluster.
Run "kubectl apply -f [podnetwork].yaml" with one of the options listed at:
  https://kubernetes.io/docs/concepts/cluster-administration/addons/

Then you can join any number of worker nodes by running the following on each as root:

kubeadm join 192.168.0.47:6443 --token 6822q4.hjlc4lv3ctgvaooj \
	--discovery-token-ca-cert-hash sha256:26b4855b227c9e491015d9b7e55e3ef05b91b506febb8d2f6d4925d168bbbac9
```

## Worker node

master node에 조인해본다.

```bash
sudo kubeadm join 192.168.0.47:6443 --token 7zgp0o.wvwp832l50abpmfy \
	--discovery-token-ca-cert-hash sha256:b433fd5f8328bf5540d0a3aa0a18abaffed3c974a509d63703dbdaffa096e019

[ERROR CRI]: container runtime is not running
```

오류가 난다면

/etc/containerd/config.toml 파일에서 

disabled_plugins 항목에서 CRI 제거 후 재시작

```bash
sudo vi /etc/containerd/config.toml

sudo systemctl restart containerd
```

토근이 유효하지 않다면

```bash
# 토큰 확인
kubeadm token list
# 기존 토큰 삭제 (위 list에서 나온 TOKEN을 넣어줌)
kubeadm token delete {TOKEN}
# 새 토큰 create
kubeadm token create --print-join-command
# 새 토큰 확인
kubeadm token list
```

하지만

```bash
kubectl get nodes
NAME         STATUS     ROLES           AGE     VERSION
jgkim-idea   NotReady   control-plane   13h     v1.28.1
node1        Ready      <none>          3m50s   v1.28.1
```

마스터 노드가 NotReady 상태이다.

```bash
kubectl get pods -n kube-system -o wide

NAME                                 READY   STATUS    RESTARTS       AGE     IP             NODE         NOMINATED NODE   READINESS GATES
coredns-5dd5756b68-pk4kl             1/1     Running   3 (54s ago)    13h     10.244.1.8     node1        <none>           <none>
coredns-5dd5756b68-v8nd8             1/1     Running   3 (53s ago)    13h     10.244.1.9     node1        <none>           <none>
etcd-jgkim-idea                      1/1     Running   16 (12h ago)   13h     192.168.0.47   jgkim-idea   <none>           <none>
kube-apiserver-jgkim-idea            1/1     Running   25 (12h ago)   13h     192.168.0.47   jgkim-idea   <none>           <none>
kube-controller-manager-jgkim-idea   1/1     Running   30 (12h ago)   13h     192.168.0.47   jgkim-idea   <none>           <none>
kube-proxy-5k64p                     1/1     Running   4 (107s ago)   5m35s   10.0.2.15      node1        <none>           <none>
kube-proxy-q88c5                     1/1     Running   10 (12h ago)   13h     192.168.0.47   jgkim-idea   <none>           <none>
kube-scheduler-jgkim-idea            1/1     Running   26 (12h ago)   13h     192.168.0.47   jgkim-idea   <none>           <none>
```

## 전부 날리고 다시

```bash
# Docker 초기화
docker rm -f `docker ps -aq`
docker volume rm `docker volume ls -q`
sudo umount /var/lib/docker/volumes
sudo rm -rf /var/lib/docker/
sudo systemctl restart docker

# kubeadm 초기화
sudo kubeadm reset
sudo systemctl restart kubelet
sudo reboot
```

## 오류시

```bash
kubectl describe node k8smaster.example.net

Name:               k8smaster.example.net
Roles:              control-plane
...
Conditions:
  Type             Status  LastHeartbeatTime                 LastTransitionTime                Reason                       Message
  ----             ------  -----------------                 ------------------                ------                       -------
...
12:28:22 +0900   KubeletNotReady              container runtime network not ready: NetworkReady=false reason:NetworkPluginNotReady message:Network plugin returns error: cni plugin not initialized
```

```bash
kubectl apply -f https://raw.githubusercontent.com/coreos/flannel/master/Documentation/kube-flannel.yml 

#재시작
systemctl restart kubelet 
```

토큰값 확인하고

```bash
kubeadm token list

TOKEN                     TTL         EXPIRES                USAGES                   DESCRIPTION                                                EXTRA GROUPS
7zgp0o.wvwp832l50abpmfy   23h         2023-09-11T03:28:26Z   authentication,signing   
```

Woker Node에서도 리셋하고 다시 조인

```bash
sudo kubeadm reset
sudo systemctl restart kubelet

sudo kubeadm join 192.168.0.47:6443 --token 7zgp0o.wvwp832l50abpmfy \
	--discovery-token-ca-cert-hash sha256:b433fd5f8328bf5540d0a3aa0a18abaffed3c974a509d63703dbdaffa096e019
```

## [다음, 배포 연습](./k8s_2.md)

[def]: #k8s