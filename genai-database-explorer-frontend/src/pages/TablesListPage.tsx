import { useState, useMemo } from 'react';
import { Title3 } from '@fluentui/react-components';
import { useTablesList } from '../hooks/useTables';
import { SearchInput } from '../components/common/SearchInput';
import { Pagination } from '../components/common/Pagination';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { ErrorBanner } from '../components/common/ErrorBanner';
import { EntityList } from '../components/entities/EntityList';

export function TablesListPage() {
  const [offset, setOffset] = useState(0);
  const [search, setSearch] = useState('');
  const limit = 50;
  const { data, isLoading, error } = useTablesList(0, 500);

  const filtered = useMemo(() => {
    if (!data) return [];
    if (!search) return data.items;
    const term = search.toLowerCase();
    return data.items.filter(
      (t) =>
        t.name.toLowerCase().includes(term) ||
        t.schema.toLowerCase().includes(term) ||
        t.description?.toLowerCase().includes(term),
    );
  }, [data, search]);

  const paged = useMemo(() => filtered.slice(offset, offset + limit), [filtered, offset, limit]);

  if (isLoading) return <LoadingSpinner label="Loading tables..." />;
  if (error) return <ErrorBanner error={error} />;

  return (
    <div className="space-y-4">
      <Title3>Tables</Title3>
      <SearchInput
        value={search}
        onChange={(v) => {
          setSearch(v);
          setOffset(0);
        }}
        placeholder="Search tables..."
      />
      <EntityList items={paged} entityType="tables" />
      <Pagination totalCount={filtered.length} offset={offset} limit={limit} onChange={setOffset} />
    </div>
  );
}
