# Docker Compose

- [Docker Compose](#docker-compose)
  - [Run](#run)
  - [MySql](#mysql)
  - [mysql-exporter](#mysql-exporter)
  - [RabbitMQ](#rabbitmq)
  - [Redis](#redis)
  - [redis-exporter](#redis-exporter)
  - [Prometheus](#prometheus)
  - [Grafana](#grafana)
  - [Influxdb](#influxdb)
  - [Portainer](#portainer)

## Run

```bash
docker-compose up -d
```

## MySql

[https://www.mysql.com/](https://www.mysql.com/)
image [https://hub.docker.com/_/mysql](https://hub.docker.com/_/mysql)
Port 3306
User/Password root/1234

## mysql-exporter

[mysql-exporter](https://github.com/prometheus/mysqld_exporter)
Port 9104

Connect to MySql

```bash
mysql -u root -p
use mysql
```

```sql
CREATE USER 'exporter'@'%' IDENTIFIED BY '1234qwer' WITH MAX_USER_CONNECTIONS 3;
GRANT ALL PRIVILEGES ON *.* TO 'exporter'@'%';
FLUSH PRIVILEGES;
```

[./mysqlexporter/.my.cnf](./mysqlexporter/.my.cnf)

```bash
user=exporter
password=1234qwer
```

## RabbitMQ

[RabbitMQ](https://www.rabbitmq.com/)
image [rabbitmq](https://hub.docker.com/rabbitmq)
Port 5672/15672
User/Password user/1234
[http://localhost:15672](http://localhost:15672)

## Redis

[https://redis.io](https://redis.io/)
Image [https://hub.docker.com/_/redis](https://hub.docker.com/_/redis)
Port 6379/6380

## redis-exporter

image oliver006/redis_exporter
ports 9121

## Prometheus

[Prometheus](https://prometheus.io/)
Image [prometheus](https://hub.docker.com/r/prom/prometheus)
Port 9090
[http://localhost:9090/](http://localhost:9090/)

## Grafana

[Grapana](https://grafana.com/)
Image [grapana](https://hub.docker.com/r/grafana/grafana)
Port 3000
User/Password admin/admin
[http://localhost:3000/login](http://localhost:3000/login)

## Influxdb

[Influxdb](https://www.influxdata.com/)
Image [influxdb](https://hub.docker.com/_/influxdb)
Port 8086
[http://localhost:8086/](http://localhost:8086/)

## Portainer

[portainer](https://www.portainer.io/)
Image [portainer](https://hub.docker.com/r/portainer/portainer)
Port 9000
[http://localhost:9000/](http://localhost:9000/)
