import { MainLayout } from '@/components/layout/MainLayout'

export default function Home() {
  return (
    <MainLayout>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Home</h1>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
            Demo.Admin을 참고하여 만든 React 관리자 인터페이스입니다.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {/* 기능 카드들 */}
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-6">
            <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
              사용자 관리
            </h3>
            <p className="text-gray-600 dark:text-gray-400 text-sm">
              사용자 목록을 조회하고 검색할 수 있습니다.
            </p>
          </div>

          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-6">
            <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
              다크 모드
            </h3>
            <p className="text-gray-600 dark:text-gray-400 text-sm">
              상단의 아이콘을 클릭하여 다크/라이트 모드를 전환할 수 있습니다.
            </p>
          </div>

          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-6">
            <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
              반응형 디자인
            </h3>
            <p className="text-gray-600 dark:text-gray-400 text-sm">
              Tailwind CSS를 사용한 모바일 친화적인 인터페이스입니다.
            </p>
          </div>
        </div>

        <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-6">
          <h3 className="text-lg font-medium text-blue-900 dark:text-blue-100 mb-2">
            기술 스택
          </h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div className="text-blue-800 dark:text-blue-200">
              <strong>Next.js 15</strong><br />
              React 프레임워크
            </div>
            <div className="text-blue-800 dark:text-blue-200">
              <strong>TypeScript</strong><br />
              타입 안정성
            </div>
            <div className="text-blue-800 dark:text-blue-200">
              <strong>Tailwind CSS</strong><br />
              유틸리티 CSS
            </div>
            <div className="text-blue-800 dark:text-blue-200">
              <strong>Zustand</strong><br />
              상태 관리
            </div>
          </div>
        </div>
      </div>
    </MainLayout>
  )
}
