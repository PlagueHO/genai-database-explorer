import { Button, Text } from '@fluentui/react-components';
import { ChevronLeft24Regular, ChevronRight24Regular } from '@fluentui/react-icons';

interface PaginationProps {
  totalCount: number;
  offset: number;
  limit: number;
  onChange: (offset: number) => void;
}

export function Pagination({ totalCount, offset, limit, onChange }: PaginationProps) {
  const currentPage = Math.floor(offset / limit) + 1;
  const totalPages = Math.max(1, Math.ceil(totalCount / limit));

  return (
    <div className="flex items-center gap-2 py-2">
      <Button
        icon={<ChevronLeft24Regular />}
        appearance="subtle"
        disabled={offset === 0}
        onClick={() => onChange(Math.max(0, offset - limit))}
        aria-label="Previous page"
      />
      <Text size={300}>
        Page {currentPage} of {totalPages}
      </Text>
      <Button
        icon={<ChevronRight24Regular />}
        appearance="subtle"
        disabled={offset + limit >= totalCount}
        onClick={() => onChange(offset + limit)}
        aria-label="Next page"
      />
    </div>
  );
}
