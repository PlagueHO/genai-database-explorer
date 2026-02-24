import { createContext, useContext, useState, type ReactNode } from 'react';

interface AppUIState {
  sidebarCollapsed: boolean;
  setSidebarCollapsed: (collapsed: boolean) => void;
  chatPanelOpen: boolean;
  setChatPanelOpen: (open: boolean) => void;
}

const AppUIContext = createContext<AppUIState | null>(null);

export function AppUIProvider({ children }: { children: ReactNode }) {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [chatPanelOpen, setChatPanelOpen] = useState(false);

  return (
    <AppUIContext.Provider
      value={{ sidebarCollapsed, setSidebarCollapsed, chatPanelOpen, setChatPanelOpen }}
    >
      {children}
    </AppUIContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAppUI(): AppUIState {
  const context = useContext(AppUIContext);
  if (!context) {
    throw new Error('useAppUI must be used within an AppUIProvider');
  }
  return context;
}
