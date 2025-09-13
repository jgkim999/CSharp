'use client'

import { useState, useEffect } from 'react'
import { MainLayout } from '@/components/layout/MainLayout'
import { ClockIcon } from '@heroicons/react/24/outline'

export default function ServerTimePage() {
  const [currentTime, setCurrentTime] = useState<Date | null>(null)

  useEffect(() => {
    // 현재 시간을 주기적으로 업데이트
    const updateTime = () => {
      setCurrentTime(new Date())
    }

    // 초기 설정
    updateTime()
    
    // 1초마다 시간 업데이트
    const interval = setInterval(updateTime, 1000)

    return () => clearInterval(interval)
  }, [])

  const formatTime = (date: Date) => {
    return date.toLocaleString('ko-KR', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    })
  }

  const formatTimeOnly = (date: Date) => {
    return date.toLocaleTimeString('ko-KR', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    })
  }

  return (
    <MainLayout>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">서버 시간</h1>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
            현재 서버 시간을 실시간으로 확인할 수 있습니다.
          </p>
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          {/* 현재 시간 */}
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-8">
            <div className="text-center space-y-4">
              <ClockIcon className="h-12 w-12 text-blue-500 mx-auto" />
              <div>
                <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                  현재 시간
                </h3>
                <div className="text-3xl font-mono font-bold text-blue-600 dark:text-blue-400">
                  {currentTime ? formatTimeOnly(currentTime) : '--:--:--'}
                </div>
              </div>
            </div>
          </div>

          {/* 전체 날짜/시간 */}
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-8">
            <div className="text-center space-y-4">
              <div>
                <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                  전체 정보
                </h3>
                <div className="text-lg font-mono text-gray-700 dark:text-gray-300">
                  {currentTime ? formatTime(currentTime) : '로딩 중...'}
                </div>
              </div>
              <div className="pt-4 border-t border-gray-200 dark:border-gray-600">
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <div className="text-gray-500 dark:text-gray-400">타임존</div>
                    <div className="font-medium text-gray-900 dark:text-white">
                      {currentTime ? Intl.DateTimeFormat().resolvedOptions().timeZone : '---'}
                    </div>
                  </div>
                  <div>
                    <div className="text-gray-500 dark:text-gray-400">UTC 오프셋</div>
                    <div className="font-medium text-gray-900 dark:text-white">
                      {currentTime ? `UTC${currentTime.getTimezoneOffset() <= 0 ? '+' : '-'}${Math.abs(currentTime.getTimezoneOffset() / 60)}` : '---'}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* 추가 정보 */}
        <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-6">
          <h3 className="text-lg font-medium text-blue-900 dark:text-blue-100 mb-2">
            참고 사항
          </h3>
          <p className="text-blue-800 dark:text-blue-200 text-sm">
            이 시간은 클라이언트 브라우저의 로컬 시간을 기준으로 표시됩니다. 
            실제 운영 환경에서는 서버 API를 통해 서버의 정확한 시간을 가져올 수 있습니다.
          </p>
        </div>
      </div>
    </MainLayout>
  )
}