'use client'

import { MainLayout } from '@/components/layout/MainLayout'
import { CloudIcon, SunIcon, CloudArrowDownIcon } from '@heroicons/react/24/outline'

// 예시 날씨 데이터
const weatherData = [
  { date: '2024-01-15', temperature: 25, condition: 'Sunny', icon: SunIcon },
  { date: '2024-01-16', temperature: 22, condition: 'Cloudy', icon: CloudIcon },
  { date: '2024-01-17', temperature: 18, condition: 'Rainy', icon: CloudArrowDownIcon },
  { date: '2024-01-18', temperature: 28, condition: 'Sunny', icon: SunIcon },
  { date: '2024-01-19', temperature: 20, condition: 'Cloudy', icon: CloudIcon },
]

export default function WeatherPage() {
  return (
    <MainLayout>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Weather</h1>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
            예시 날씨 정보를 확인할 수 있습니다.
          </p>
        </div>

        <div className="grid gap-4">
          {weatherData.map((weather, index) => {
            const IconComponent = weather.icon
            return (
              <div
                key={index}
                className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-6"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-4">
                    <IconComponent className="h-8 w-8 text-blue-500" />
                    <div>
                      <div className="text-lg font-medium text-gray-900 dark:text-white">
                        {weather.condition}
                      </div>
                      <div className="text-sm text-gray-500 dark:text-gray-400">
                        {new Date(weather.date).toLocaleDateString('ko-KR', {
                          year: 'numeric',
                          month: 'long',
                          day: 'numeric',
                        })}
                      </div>
                    </div>
                  </div>
                  <div className="text-3xl font-bold text-gray-900 dark:text-white">
                    {weather.temperature}°C
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </MainLayout>
  )
}