# 쿠버네티스 삭제

- [쿠버네티스 삭제](#쿠버네티스-삭제)
  - [삭제](#삭제)

## 삭제

```bash
sudo kubeadm reset
```

```bash
sudo systemctl stop kubelet
sudo systemctl stop docker

sudo ip link delete cni0  
sudo ip link delete flannel.1
```

```bash
sudo rm -rf /var/lib/cni/
sudo rm -rf /var/lib/kubelet/*
sudo rm -rf /var/lib/etcd/
sudo rm -rf /run/flannel/
sudo rm -rf /etc/cni/
sudo rm -rf /etc/kubernetes/
sudo rm -rf ~/.kube
```

```bash
sudo apt-get purge kubeadm kubectl kubelet kubernetes-cni kube*
sudo apt-get autoremove
```