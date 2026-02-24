import { NavDrawer, NavDrawerBody, NavItem } from '@fluentui/react-components';
import {
  Home24Regular,
  Table24Regular,
  Eye24Regular,
  Code24Regular,
  ArrowSync24Regular,
} from '@fluentui/react-icons';
import { useNavigate, useLocation } from 'react-router';
import { useAppUI } from '../../context/AppUIContext';
import { useReloadModel } from '../../hooks/useModel';
import { Button, Spinner } from '@fluentui/react-components';

export function Sidebar() {
  const navigate = useNavigate();
  const location = useLocation();
  const { sidebarCollapsed } = useAppUI();
  const reloadModel = useReloadModel();

  const navItems = [
    { value: '/', icon: <Home24Regular />, label: 'Dashboard' },
    { value: '/tables', icon: <Table24Regular />, label: 'Tables' },
    { value: '/views', icon: <Eye24Regular />, label: 'Views' },
    { value: '/stored-procedures', icon: <Code24Regular />, label: 'Stored Procedures' },
  ];

  const selectedValue =
    navItems.find((item) => item.value !== '/' && location.pathname.startsWith(item.value))
      ?.value ?? '/';

  return (
    <NavDrawer
      open={!sidebarCollapsed}
      type="inline"
      className="h-full border-r"
      selectedValue={selectedValue}
    >
      <NavDrawerBody>
        {navItems.map((item) => (
          <NavItem
            key={item.value}
            value={item.value}
            icon={item.icon}
            onClick={() => navigate(item.value)}
          >
            {item.label}
          </NavItem>
        ))}
        <div className="mt-4 px-3">
          <Button
            icon={reloadModel.isPending ? <Spinner size="tiny" /> : <ArrowSync24Regular />}
            appearance="subtle"
            size="small"
            disabled={reloadModel.isPending}
            onClick={() => reloadModel.mutate()}
            className="w-full"
          >
            Reload Model
          </Button>
        </div>
      </NavDrawerBody>
    </NavDrawer>
  );
}
