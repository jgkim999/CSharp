'use client'

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { 
  HomeIcon,
  PlusIcon,
  ListBulletIcon,
  UsersIcon,
  ClockIcon
} from '@heroicons/react/24/outline'
import { cn } from '@/lib/utils'

const navigation = [
  { name: 'Home', href: '/', icon: HomeIcon },
  { name: 'Counter', href: '/counter', icon: PlusIcon },
  { name: 'Weather', href: '/weather', icon: ListBulletIcon },
  { name: 'User', href: '/user', icon: UsersIcon },
  { name: '서버 시간', href: '/servertime', icon: ClockIcon },
]

export function NavMenu() {
  const pathname = usePathname()

  return (
    <nav className="p-4">
      <ul className="space-y-2">
        {navigation.map((item) => {
          const Icon = item.icon
          const isActive = pathname === item.href || (item.href !== '/' && pathname.startsWith(item.href))
          
          return (
            <li key={item.name}>
              <Link
                href={item.href}
                className={cn(
                  "flex items-center space-x-3 px-3 py-2 rounded-md text-sm font-medium transition-colors",
                  isActive
                    ? "bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300"
                    : "text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white"
                )}
              >
                <Icon className="h-5 w-5" />
                <span>{item.name}</span>
              </Link>
            </li>
          )
        })}
      </ul>
    </nav>
  )
}