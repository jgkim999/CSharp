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

## MySQL

```sql
CREATE USER 'user1'@'%' IDENTIFIED BY '1234';
GRANT ALL PRIVILEGES ON *.* TO 'user1'@'%';
FLUSH PRIVILEGES;
```

For MySQL Exporter

```sql
CREATE USER 'exporter'@'%' IDENTIFIED BY '1234qwer' WITH MAX_USER_CONNECTIONS 3;
GRANT ALL PRIVILEGES ON *.* TO 'exporter'@'%';
FLUSH PRIVILEGES;
```

## Redis benchmarek

```bash

SET: 66445.18 requests per second, p50=0.423 msec
GET: 66577.89 requests per second, p50=0.423 msec
INCR: 66720.05 requests per second, p50=0.423 msec
LPUSH: 66418.70 requests per second, p50=0.423 msec
RPUSH: 66471.69 requests per second, p50=0.423 msec
LPOP: 66299.80 requests per second, p50=0.431 msec
RPOP: 66476.10 requests per second, p50=0.423 msec
SADD: 66992.70 requests per second, p50=0.423 msec
HSET: 66067.66 requests per second, p50=0.423 msec
SPOP: 66997.19 requests per second, p50=0.423 msec
ZADD: 66247.10 requests per second, p50=0.423 msec
ZPOPMIN: 66822.59 requests per second, p50=0.423 msec
LPUSH (needed to benchmark LRANGE): 66106.96 requests per second, p50=0.431 msec
LRANGE_100 (first 100 elements): 51069.92 requests per second, p50=0.527 msec
LRANGE_300 (first 300 elements): 27719.26 requests per second, p50=0.911 msec
LRANGE_500 (first 500 elements): 20118.70 requests per second, p50=1.239 msec
LRANGE_600 (first 600 elements): 17676.28 requests per second, p50=1.399 msec
MSET (10 keys): 65445.03 requests per second, p50=0.447 msec
XADD: 66058.93 requests per second, p50=0.431 msec

```