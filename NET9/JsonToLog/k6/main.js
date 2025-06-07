import http from 'k6/http';
import { fail, sleep } from 'k6';
import { randomString, randomIntBetween, check } from 'https://jslib.k6.io/k6-utils/1.5.0/index.js';

export const options = {
    thresholds: {
        http_req_failed: [{ threshold: 'rate<0.01', abortOnFail: true }],
        http_req_duration: ['p(99)<1000'],
    },
    scenarios: {
        // define scenarios
        breaking: {
            executor: 'ramping-vus',
            stages: [
                { duration: '1s', target: 1 },
                { duration: '1h', target: 1 },
                { duration: '1s', target: 0 }
            ],
        },
    },
};

export default function () {
    const baseUrl = 'http://localhost:5000';
    
    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.get(`${baseUrl}/api/fake/v1?count=2`, params);
    sleep(randomIntBetween(1, 1));    
}
