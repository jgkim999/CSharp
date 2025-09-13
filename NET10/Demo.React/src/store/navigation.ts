import { create } from 'zustand'

interface NavigationState {
  isDrawerOpen: boolean
  toggleDrawer: () => void
  setDrawerOpen: (open: boolean) => void
}

export const useNavigationStore = create<NavigationState>((set) => ({
  isDrawerOpen: true, // Demo.Admin과 동일하게 기본값을 true로 설정
  toggleDrawer: () => set((state) => ({ isDrawerOpen: !state.isDrawerOpen })),
  setDrawerOpen: (open: boolean) => set({ isDrawerOpen: open }),
}))