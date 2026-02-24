import { useState, useMemo } from 'react';
import { Title3 } from '@fluentui/react-components';
import { useViewsList } from '../hooks/useViews';
import { SearchInput } from '../components/common/SearchInput';
import { Pagination } from '../components/common/Pagination';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { ErrorBanner } from '../components/common/ErrorBanner';
import { EntityList } from '../components/entities/EntityList';

export function ViewsListPage() {
  const [offset, setOffset] = useState(0);
  const [search, setSearch] = useState('');
  const limit = 50;
  const { data, isLoading, error } = useViewsList(0, 500);

  const filtered = useMemo(() => {
    if (!data) return [];
    if (!search) return data.items;
    const term = search.toLowerCase();
    return data.items.filter(
      (v) =>
        v.name.toLowerCase().includes(term) ||
        v.schema.toLowerCase().includes(term) ||
        v.description?.toLowerCase().includes(term),
    );
  }, [data, search]);

  const paged = useMemo(() => filtered.slice(offset, offset + limit), [filtered, offset, limit]);

  if (isLoading) return <LoadingSpinner label="Loading views..." />;
  if (error) return <ErrorBanner error={error} />;

  return (
    <div className="space-y-4">
      <Title3>Views</Title3>
      <SearchInput
        value={search}
        onChange={(v) => {
          setSearch(v);
          setOffset(0);
        }}
        placeholder="Search views..."
      />
      <EntityList items={paged} entityType="views" />
      <Pagination totalCount={filtered.length} offset={offset} limit={limit} onChange={setOffset} />
    </div>
  );
}
