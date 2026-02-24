import { BrowserRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { AppUIProvider } from './context/AppUIContext';
import { AppLayout } from './components/layout/AppLayout';
import { DashboardPage } from './pages/DashboardPage';
import { TablesListPage } from './pages/TablesListPage';
import { TableDetailPage } from './pages/TableDetailPage';
import { ViewsListPage } from './pages/ViewsListPage';
import { ViewDetailPage } from './pages/ViewDetailPage';
import { StoredProceduresListPage } from './pages/StoredProceduresListPage';
import { StoredProcedureDetailPage } from './pages/StoredProcedureDetailPage';

const queryClient = new QueryClient();

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <FluentProvider theme={webLightTheme} className="h-full">
        <AppUIProvider>
          <BrowserRouter>
            <Routes>
              <Route element={<AppLayout />}>
                <Route index element={<DashboardPage />} />
                <Route path="tables" element={<TablesListPage />} />
                <Route path="tables/:schema/:name" element={<TableDetailPage />} />
                <Route path="views" element={<ViewsListPage />} />
                <Route path="views/:schema/:name" element={<ViewDetailPage />} />
                <Route path="stored-procedures" element={<StoredProceduresListPage />} />
                <Route
                  path="stored-procedures/:schema/:name"
                  element={<StoredProcedureDetailPage />}
                />
              </Route>
            </Routes>
          </BrowserRouter>
        </AppUIProvider>
      </FluentProvider>
    </QueryClientProvider>
  );
}

export default App;
