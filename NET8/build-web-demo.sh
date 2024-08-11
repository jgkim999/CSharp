docker build -t web-demo -f DockerfileWebDemo .
docker run -d -p 0:5003 web-demo:latest
