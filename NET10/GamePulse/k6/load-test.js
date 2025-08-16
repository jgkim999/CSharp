import http from 'k6/http';
import { check, sleep } from 'k6';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

export const options = {
  stages: [
    { duration: '1m', target: 10 },
    { duration: '360m', target: 10 },
    { duration: '10s', target: 0 },
  ],
};

export default function () {
  const url = 'http://localhost:5198/api/sod/rtt/v1';
  const payload = JSON.stringify({
      type: "client",
      rtt: randomIntBetween(8, 500),
      quality: randomIntBetween(1, 4)
  });
  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  const response = http.post(url, payload, params);

  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(0.5);
}
