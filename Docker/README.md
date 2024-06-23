# Docker Compose

```bash
docker-compose up -d
```

| Service | container_name | Port | Password | URL |
|---------|----------------|------|----------|-----|
| [MySql](https://www.mysql.com/) |  [mysql](https://hub.docker.com/_/mysql) | 3306 | 1234 | |
| [RabbitMQ](https://www.rabbitmq.com/) | [rabbitmq](https://hub.docker.com/_/rabbitmq) | 5672/15672 | user/1234 | http://localhost:15672/ |
| [Redis](https://redis.io/) | [redis](https://hub.docker.com/_/redis) | 6379/6380 | | |
| [Prometheus](https://prometheus.io/) | [prometheus](https://hub.docker.com/r/prom/prometheus) | 9090 | | http://localhost:9090/ |
| [Grapana](https://grafana.com/) | [grapana](https://hub.docker.com/r/grafana/grafana) | 3000 | admin/admin | http://localhost:3000/login |
| [Influxdb](https://www.influxdata.com/) | [influxdb](https://hub.docker.com/_/influxdb) | 8086 | | http://localhost:8086/ |

## MySQL Exporter

```sql
CREATE USER 'exporter'@'%' IDENTIFIED BY '1234qwer' WITH MAX_USER_CONNECTIONS 3;
GRANT ALL PRIVILEGES ON *.* TO 'exporter'@'%';
FLUSH PRIVILEGES;
```
