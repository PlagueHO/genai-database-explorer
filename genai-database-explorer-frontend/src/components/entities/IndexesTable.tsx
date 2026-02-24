import {
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Badge,
  Text,
} from '@fluentui/react-components';
import type { Index } from '../../types/api';

interface IndexesTableProps {
  indexes: readonly Index[];
}

export function IndexesTable({ indexes }: IndexesTableProps) {
  if (indexes.length === 0) return null;

  return (
    <Table aria-label="Indexes" className="mt-4">
      <TableHeader>
        <TableRow>
          <TableHeaderCell>Name</TableHeaderCell>
          <TableHeaderCell>Type</TableHeaderCell>
          <TableHeaderCell>Column</TableHeaderCell>
          <TableHeaderCell>Flags</TableHeaderCell>
        </TableRow>
      </TableHeader>
      <TableBody>
        {indexes.map((idx) => (
          <TableRow key={`${idx.name}-${idx.columnName}`}>
            <TableCell>
              <Text>{idx.name}</Text>
            </TableCell>
            <TableCell>
              <Text size={200}>{idx.type}</Text>
            </TableCell>
            <TableCell>
              <Text size={200}>{idx.columnName}</Text>
            </TableCell>
            <TableCell>
              <div className="flex gap-1">
                {idx.isPrimaryKey && <Badge size="small">PK</Badge>}
                {idx.isUnique && <Badge size="small">Unique</Badge>}
                {idx.isUniqueConstraint && <Badge size="small">Constraint</Badge>}
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
