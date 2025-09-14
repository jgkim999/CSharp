'use client'

import { useState } from 'react'
import { MainLayout } from '@/components/layout/MainLayout'
import { PlusIcon, MinusIcon } from '@heroicons/react/24/outline'

export default function CounterPage() {
  const [count, setCount] = useState(0)

  return (
    <MainLayout>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Counter</h1>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
            간단한 카운터 예제입니다.
          </p>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-8">
          <div className="text-center space-y-6">
            <div className="text-6xl font-bold text-blue-600 dark:text-blue-400 font-mono">
              {count}
            </div>
            
            <div className="flex justify-center space-x-4">
              <button
                onClick={() => setCount(count - 1)}
                className="inline-flex items-center px-6 py-3 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 dark:focus:ring-offset-gray-800 transition-colors"
              >
                <MinusIcon className="h-5 w-5 mr-2" />
                Decrement
              </button>
              
              <button
                onClick={() => setCount(count + 1)}
                className="inline-flex items-center px-6 py-3 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 dark:focus:ring-offset-gray-800 transition-colors"
              >
                <PlusIcon className="h-5 w-5 mr-2" />
                Increment
              </button>
            </div>
            
            <button
              onClick={() => setCount(0)}
              className="px-4 py-2 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white transition-colors"
            >
              Reset
            </button>
          </div>
        </div>
      </div>
    </MainLayout>
  )
}