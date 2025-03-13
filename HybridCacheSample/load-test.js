import http from 'k6/http';
import {check, sleep} from 'k6';

export const options = {
  vus: 100,
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01']
  }
};

export default function () {
  const response = http.get('http://localhost:8080/.well-known/openid-configuration', {
    insecureSkipTLSVerify: true
  });

  check(response, {
    'status is 200': (r) => r.status === 200,
    'response is valid': (r) => r.json() !== null
  });

  sleep(1);
}
