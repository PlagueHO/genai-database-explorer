import { useNavigate } from 'react-router';
import { Badge, Text } from '@fluentui/react-components';
import type { EntitySummary } from '../../types/api';
import { EmptyState } from '../common/EmptyState';

interface EntityListProps {
  items: readonly EntitySummary[];
  entityType: 'tables' | 'views' | 'stored-procedures';
}

export function EntityList({ items, entityType }: EntityListProps) {
  const navigate = useNavigate();

  if (items.length === 0) {
    return <EmptyState message={`No ${entityType.replace('-', ' ')} found.`} />;
  }

  return (
    <div className="flex flex-col gap-1">
      {items.map((item) => (
        <div
          key={`${item.schema}.${item.name}`}
          className="flex items-center gap-3 p-3 rounded-md cursor-pointer hover:bg-gray-100"
          onClick={() => navigate(`/${entityType}/${item.schema}/${item.name}`)}
          role="link"
          tabIndex={0}
          onKeyDown={(e) =>
            e.key === 'Enter' && navigate(`/${entityType}/${item.schema}/${item.name}`)
          }
        >
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <Text weight="semibold">
                {item.schema}.{item.name}
              </Text>
              {item.notUsed && (
                <Badge appearance="filled" color="warning" size="small">
                  Not Used
                </Badge>
              )}
            </div>
            {item.description && (
              <Text size={200} className="text-gray-600 truncate block">
                {item.description}
              </Text>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
