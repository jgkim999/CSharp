import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    iterations: 100,
};

export default function () {
    const response = http.get('https://test-api.k6.io/public/crocodiles/');
    sleep(10);
}
