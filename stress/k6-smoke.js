// 答辩录屏用：先 dotnet run，再执行 k6 run stress/k6-smoke.js
// 安装: https://grafana.com/docs/k6/latest/set-up/install-k6/
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 10,
  duration: '20s',
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<3000'],
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:5001';

export default function () {
  const r1 = http.get(`${BASE}/api/Import/price-table/template`);
  check(r1, { 'template 200': (r) => r.status === 200 });
  sleep(0.2);
  const r2 = http.get(`${BASE}/`);
  check(r2, { 'index 200': (r) => r.status === 200 });
}
