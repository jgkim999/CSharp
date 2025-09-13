'use client'

import { Fragment } from 'react'
import { Dialog, Transition } from '@headlessui/react'
import { ExclamationTriangleIcon } from '@heroicons/react/24/outline'

interface ErrorDialogProps {
  isOpen: boolean
  onClose: () => void
  title?: string
  message: string
  url?: string
  method?: string
  status?: number
  statusText?: string
}

export function ErrorDialog({
  isOpen,
  onClose,
  title = '오류 발생',
  message,
  url,
  method,
  status,
  statusText,
}: ErrorDialogProps) {
  return (
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black bg-opacity-25" />
        </Transition.Child>

        <div className="fixed inset-0 overflow-y-auto">
          <div className="flex min-h-full items-center justify-center p-4 text-center">
            <Transition.Child
              as={Fragment}
              enter="ease-out duration-300"
              enterFrom="opacity-0 scale-95"
              enterTo="opacity-100 scale-100"
              leave="ease-in duration-200"
              leaveFrom="opacity-100 scale-100"
              leaveTo="opacity-0 scale-95"
            >
              <Dialog.Panel className="w-full max-w-md transform overflow-hidden rounded-2xl bg-white dark:bg-gray-800 p-6 text-left align-middle shadow-xl transition-all">
                <div className="flex items-center">
                  <div className="flex-shrink-0">
                    <ExclamationTriangleIcon className="h-6 w-6 text-red-600 dark:text-red-400" />
                  </div>
                  <div className="ml-3">
                    <Dialog.Title
                      as="h3"
                      className="text-lg font-medium leading-6 text-gray-900 dark:text-white"
                    >
                      {title}
                    </Dialog.Title>
                  </div>
                </div>

                <div className="mt-4">
                  <p className="text-sm text-gray-700 dark:text-gray-300 mb-4">
                    {message}
                  </p>

                  {(url || method || status) && (
                    <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-3 mb-4">
                      <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-2">
                        요청 정보
                      </h4>
                      {method && (
                        <div className="text-xs text-gray-600 dark:text-gray-400 mb-1">
                          <span className="font-medium">Method:</span> {method}
                        </div>
                      )}
                      {url && (
                        <div className="text-xs text-gray-600 dark:text-gray-400 mb-1 break-all">
                          <span className="font-medium">URL:</span> {url}
                        </div>
                      )}
                      {status && (
                        <div className="text-xs text-gray-600 dark:text-gray-400">
                          <span className="font-medium">Status:</span> {status}
                          {statusText && ` (${statusText})`}
                        </div>
                      )}
                    </div>
                  )}
                </div>

                <div className="mt-6">
                  <button
                    type="button"
                    className="inline-flex justify-center rounded-md border border-transparent bg-red-100 px-4 py-2 text-sm font-medium text-red-900 hover:bg-red-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-red-500 focus-visible:ring-offset-2 dark:bg-red-900 dark:text-red-100 dark:hover:bg-red-800"
                    onClick={onClose}
                  >
                    확인
                  </button>
                </div>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  )
}