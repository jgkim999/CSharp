import http from "k6/http";
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

export const options = {
  stages: [
    { duration: '1m', target: 10 },
    { duration: '1m', target: 20 },
    { duration: '1m', target: 0 },
  ]
};

export default function () {
  const payload = JSON.stringify({
    name: 'test' + randomIntBetween(1, 1000)
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };
  const response = http.post("https://localhost:5000/api/v1/login", payload, params);
  check(response, {
    'login succeeded': (r) => r.status === 200,
  });
}
