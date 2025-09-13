'use client'

import { useState, useCallback, useMemo, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ColumnDef, PaginationState } from '@tanstack/react-table'
import { MagnifyingGlassIcon } from '@heroicons/react/24/outline'
import { MainLayout } from '@/components/layout/MainLayout'
import { DataTable } from '@/components/ui/DataTable'
import { ErrorDialog } from '@/components/ui/ErrorDialog'
import { fetchUsers, UserDto, getErrorDetails } from '@/lib/api'
import { formatDate } from '@/lib/utils'
import { useLocalStorage } from '@/hooks/useLocalStorage'
import toast from 'react-hot-toast'

const USER_SEARCH_TERM_KEY = 'UserSearchTerm'
const PAGE_SIZE_KEY = 'UserPageSize'

export default function UserPage() {
  const [searchTerm, setSearchTerm] = useLocalStorage(USER_SEARCH_TERM_KEY, '')
  const [pageSize, setPageSize] = useLocalStorage(PAGE_SIZE_KEY, 10)
  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: pageSize,
  })

  // 검색어 입력 상태 (실제 검색과 분리)
  const [inputSearchTerm, setInputSearchTerm] = useState(searchTerm)
  
  // 오류 다이얼로그 상태
  const [errorDialog, setErrorDialog] = useState<{
    isOpen: boolean
    title?: string
    message: string
    url?: string
    method?: string
    status?: number
    statusText?: string
  }>({ isOpen: false, message: '' })

  // 사용자 목록 조회
  const {
    data: userResponse,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['users', searchTerm, pagination.pageIndex, pagination.pageSize],
    queryFn: () =>
      fetchUsers({
        searchTerm: searchTerm || undefined,
        page: pagination.pageIndex,
        pageSize: pagination.pageSize,
      }),
    retry: 1,
  })

  // 페이지네이션 변경 핸들러
  const handlePaginationChange = useCallback((updater: PaginationState | ((old: PaginationState) => PaginationState)) => {
    const newPagination = typeof updater === 'function' ? updater(pagination) : updater
    setPagination(newPagination)
    
    // 페이지 크기가 변경되었으면 저장
    if (newPagination.pageSize !== pageSize) {
      setPageSize(newPagination.pageSize)
    }
  }, [pagination, pageSize, setPageSize])

  // 검색 실행
  const handleSearch = useCallback(() => {
    setSearchTerm(inputSearchTerm)
    setPagination(prev => ({ ...prev, pageIndex: 0 })) // 페이지를 첫 페이지로 리셋
    
    if (inputSearchTerm.trim()) {
      toast.success(`'${inputSearchTerm}'로 검색 중...`)
    } else {
      toast.success('전체 사용자 목록을 불러오는 중...')
    }
  }, [inputSearchTerm, setSearchTerm])

  // Enter 키 핸들러
  const handleKeyPress = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch()
    }
  }, [handleSearch])

  // 테이블 컬럼 정의
  const columns: ColumnDef<UserDto>[] = useMemo(
    () => [
      {
        accessorKey: 'id',
        header: 'Id',
        cell: ({ row }) => <span className="font-mono">{row.getValue('id')}</span>,
      },
      {
        accessorKey: 'name',
        header: 'Name',
      },
      {
        accessorKey: 'email',
        header: 'Email',
        cell: ({ row }) => (
          <a
            href={`mailto:${row.getValue('email')}`}
            className="text-blue-600 dark:text-blue-400 hover:underline"
          >
            {row.getValue('email')}
          </a>
        ),
      },
      {
        accessorKey: 'createdAt',
        header: 'Created',
        cell: ({ row }) => formatDate(row.getValue('createdAt')),
      },
    ],
    []
  )

  // 에러 처리 (useEffect로 이동)
  useEffect(() => {
    if (error) {
      const errorDetails = getErrorDetails(error)
      setErrorDialog({
        isOpen: true,
        title: '사용자 목록 조회 실패',
        message: errorDetails.message,
        url: errorDetails.fullUrl,
        method: errorDetails.method,
        status: errorDetails.status,
        statusText: errorDetails.statusText,
      })
    }
  }, [error])

  // 성공 시 토스트 (useEffect로 이동)
  useEffect(() => {
    if (userResponse && !isLoading) {
      const message = searchTerm 
        ? `'${searchTerm}' 검색 완료: ${userResponse.totalItems}건 발견`
        : `사용자 목록을 성공적으로 불러왔습니다. (총 ${userResponse.totalItems}건)`
      
      // 중복 토스트 방지를 위해 조건부로 표시
      if (pagination.pageIndex === 0) {
        toast.success(message)
      }
    }
  }, [userResponse, isLoading, searchTerm, pagination.pageIndex])

  return (
    <MainLayout>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">User</h1>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
            사용자 목록을 조회하고 관리합니다.
          </p>
        </div>

        {/* 검색 영역 */}
        <div className="flex items-center space-x-3">
          <div className="flex-1">
            <input
              type="text"
              placeholder="Search by Name"
              value={inputSearchTerm}
              onChange={(e) => setInputSearchTerm(e.target.value)}
              onKeyPress={handleKeyPress}
              className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm placeholder-gray-400 dark:placeholder-gray-500 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
            />
          </div>
          <button
            onClick={handleSearch}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 dark:focus:ring-offset-gray-800 transition-colors"
          >
            <MagnifyingGlassIcon className="h-4 w-4 mr-2" />
            Search
          </button>
        </div>

        {/* 데이터 테이블 */}
        <DataTable
          columns={columns}
          data={userResponse?.items || []}
          totalItems={userResponse?.totalItems || 0}
          pagination={pagination}
          onPaginationChange={handlePaginationChange}
          isLoading={isLoading}
          manualPagination={true}
        />
        
        {/* 오류 다이얼로그 */}
        <ErrorDialog
          isOpen={errorDialog.isOpen}
          onClose={() => setErrorDialog({ isOpen: false, message: '' })}
          title={errorDialog.title}
          message={errorDialog.message}
          url={errorDialog.url}
          method={errorDialog.method}
          status={errorDialog.status}
          statusText={errorDialog.statusText}
        />
      </div>
    </MainLayout>
  )
}