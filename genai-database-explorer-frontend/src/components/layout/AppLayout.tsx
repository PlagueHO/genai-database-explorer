import { useEffect } from 'react';
import { Outlet } from 'react-router';
import { Button } from '@fluentui/react-components';
import { Navigation24Regular, Chat24Regular } from '@fluentui/react-icons';
import { useAppUI } from '../../context/AppUIContext';
import { Sidebar } from './Sidebar';
import { ChatPanel } from './ChatPanel';

export function AppLayout() {
  const { sidebarCollapsed, setSidebarCollapsed, chatPanelOpen, setChatPanelOpen } = useAppUI();

  useEffect(() => {
    const mq = window.matchMedia('(max-width: 768px)');
    const handler = (e: MediaQueryListEvent) => setSidebarCollapsed(e.matches);
    if (mq.matches) setSidebarCollapsed(true);
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, [setSidebarCollapsed]);

  return (
    <div className="flex flex-col h-full">
      {/* Top bar */}
      <header className="flex items-center gap-2 px-3 py-2 border-b bg-white">
        <Button
          icon={<Navigation24Regular />}
          appearance="subtle"
          onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
          aria-label="Toggle sidebar"
        />
        <span className="font-semibold text-lg flex-1">GenAI Database Explorer</span>
        <Button
          icon={<Chat24Regular />}
          appearance="subtle"
          onClick={() => setChatPanelOpen(!chatPanelOpen)}
          aria-label="Toggle chat panel"
        />
      </header>
      {/* Main area */}
      <div className="flex flex-1 overflow-hidden">
        <Sidebar />
        <main className="flex-1 overflow-auto p-6">
          <Outlet />
        </main>
        <ChatPanel />
      </div>
    </div>
  );
}
