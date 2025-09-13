
import axios from 'axios'

// Demo.Web API 베이스 URL (Demo.Admin과 동일한 API 사용)
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5266'

export const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000, // 10초 타임아웃
  withCredentials: true, // CORS credentials 포함
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
})

// 디버깅을 위한 interceptor 추가
api.interceptors.request.use(
  (config) => {
    console.log('🚀 API Request:', {
      method: config.method,
      url: config.url,
      baseURL: config.baseURL,
      data: config.data,
    })
    return config
  },
  (error) => {
    console.error('❌ API Request Error:', error)
    return Promise.reject(error)
  }
)

api.interceptors.response.use(
  (response) => {
    console.log('✅ API Response:', {
      status: response.status,
      url: response.config.url,
      data: response.data,
    })
    return response
  },
  (error) => {
    console.error('❌ API Response Error:', {
      message: error.message,
      status: error.response?.status,
      url: error.config?.url,
      data: error.response?.data,
    })
    return Promise.reject(error)
  }
)

// 사용자 목록 조회 API 타입
export interface UserDto {
  id: number
  name: string
  email: string
  createdAt: string
}

export interface UserListRequest {
  searchTerm?: string
  page: number
  pageSize: number
}

export interface UserListResponse {
  items: UserDto[]
  totalItems: number
}

// 사용자 목록 조회 함수
export const fetchUsers = async (request: UserListRequest): Promise<UserListResponse> => {
  const response = await api.post<UserListResponse>('/api/user/list', request)
  return response.data
}

// API 응답 에러 처리 유틸리티
export const getErrorMessage = (error: unknown): string => {
  if (axios.isAxiosError(error)) {
    return error.response?.data?.message || error.message || '알 수 없는 오류가 발생했습니다.'
  }
  return String(error)
}

// API 에러 정보 추출 유틸리티
export const getErrorDetails = (error: unknown) => {
  if (axios.isAxiosError(error)) {
    return {
      message: error.response?.data?.message || error.message || '알 수 없는 오류가 발생했습니다.',
      url: error.config?.url || '알 수 없는 URL',
      method: error.config?.method?.toUpperCase() || 'UNKNOWN',
      status: error.response?.status,
      statusText: error.response?.statusText,
      fullUrl: `${error.config?.baseURL || ''}${error.config?.url || ''}`,
    }
  }
  return {
    message: String(error),
    url: '알 수 없는 URL',
    method: 'UNKNOWN',
    status: undefined,
    statusText: undefined,
    fullUrl: '알 수 없는 URL',
  }
}