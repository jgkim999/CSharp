# .NET Practice Project

```bash
> podman ps

CONTAINER ID  IMAGE       COMMAND     CREATED     STATUS      PORTS       NAMES

> podman search mysql
NAME                                     DESCRIPTION
docker.io/library/mysql                  MySQL is a widely used, open-source relation...
...

> podman pull docker.io/library/mysql
Trying to pull docker.io/library/mysql:latest...
Getting image source signatures
...
Copying blob sha256:04bf2c116556046ae26a0c94d0bb53863e20e5ba1ca620a8a89134d159d73280
Copying config sha256:e9387c13ed83ab7915ed1cf73d505c6604c1f237b9f059ca26000ea70fa9dafb
Writing manifest to image destination
e9387c13ed83ab7915ed1cf73d505c6604c1f237b9f059ca26000ea70fa9dafb

> podman images
REPOSITORY               TAG         IMAGE ID      CREATED      SIZE
docker.io/library/mysql  latest      e9387c13ed83  2 weeks ago  594 MB

> podman run --name mysql-container -e MYSQL_ROOT_PASSWORD=1234 -d -p 3306:3306 mysql:latest
e8e6d938c07801b02974c33da59bd905b6cd4b38fbeefc6b7b859d4b3f951d5a

> podman ps -a
CONTAINER ID  IMAGE                           COMMAND     CREATED        STATUS        PORTS                   NAMES
e8e6d938c078  docker.io/library/mysql:latest  mysqld      3 minutes ago  Up 3 minutes  0.0.0.0:3306->3306/tcp  mysql-container

> kubectl apply -k ./

> kubectl get nodes
NAME                         STATUS   ROLES           AGE     VERSION
kind-cluster-control-plane   Ready    control-plane   7m25s   v1.30.0

> kubectl get pods
NAME                  READY   STATUS    RESTARTS   AGE
mysql-container-pod   0/1     Pending   0          7m

> kubectl get services
NAME         TYPE        CLUSTER-IP   EXTERNAL-IP   PORT(S)   AGE
kubernetes   ClusterIP   10.96.0.1    <none>        443/TCP   12m

> kubectl port-forward mysql-container-pod 3306:3306

```

Ubuntu

```bash
$ curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"

$ curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl.sha256"

$ sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl

$ kubectl version --client
Client Version: v1.30.1
Kustomize Version: v5.0.4-0.20230601165947-6ce0bf390ce3
```
