import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Cross-Origin 요청 허용 설정
  allowedDevOrigins: [
    '192.168.0.60',
    'localhost',
    '127.0.0.1',
  ],
  // 추가 설정 옵션들
};

export default nextConfig;
