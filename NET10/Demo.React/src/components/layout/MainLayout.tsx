'use client'

import { useEffect } from 'react'
import { useThemeStore } from '@/store/theme'
import { useNavigationStore } from '@/store/navigation'
import { Bars3Icon, SunIcon, MoonIcon, EllipsisVerticalIcon } from '@heroicons/react/24/outline'
import { NavMenu } from './NavMenu'
import { cn } from '@/lib/utils'
import { Toaster } from 'react-hot-toast'

interface MainLayoutProps {
  children: React.ReactNode
}

export function MainLayout({ children }: MainLayoutProps) {
  const { isDarkMode, toggleDarkMode } = useThemeStore()
  const { isDrawerOpen, toggleDrawer } = useNavigationStore()

  // 다크 모드 클래스 적용
  useEffect(() => {
    if (isDarkMode) {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  }, [isDarkMode])

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      {/* App Bar */}
      <header className="fixed top-0 left-0 right-0 z-40 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between h-16 px-4">
          <div className="flex items-center space-x-3">
            <button
              onClick={toggleDrawer}
              className="p-2 text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors"
            >
              <Bars3Icon className="h-6 w-6" />
            </button>
            <h1 className="text-xl font-semibold text-gray-900 dark:text-white">
              Application
            </h1>
          </div>
          
          <div className="flex items-center space-x-2">
            <button
              onClick={toggleDarkMode}
              className="p-2 text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors"
            >
              {isDarkMode ? (
                <SunIcon className="h-6 w-6" />
              ) : (
                <MoonIcon className="h-6 w-6" />
              )}
            </button>
            <button className="p-2 text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors">
              <EllipsisVerticalIcon className="h-6 w-6" />
            </button>
          </div>
        </div>
      </header>

      {/* Drawer/Sidebar */}
      <aside
        className={cn(
          "fixed top-16 left-0 z-30 w-64 h-[calc(100vh-4rem)] bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 transition-transform duration-200",
          isDrawerOpen ? "translate-x-0" : "-translate-x-full"
        )}
      >
        <NavMenu />
      </aside>

      {/* Main Content */}
      <main
        className={cn(
          "pt-16 transition-all duration-200",
          isDrawerOpen ? "ml-64" : "ml-0"
        )}
      >
        <div className="p-4">
          {children}
        </div>
      </main>

      {/* Toast Container */}
      <Toaster 
        position="bottom-right"
        toastOptions={{
          duration: 3000,
          className: 'dark:bg-gray-800 dark:text-white',
        }}
      />
    </div>
  )
}