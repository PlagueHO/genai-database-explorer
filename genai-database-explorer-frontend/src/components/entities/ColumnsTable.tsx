import { useState } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Badge,
  Button,
  Input,
  Text,
} from '@fluentui/react-components';
import { Edit24Regular, Checkmark24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import type { Column } from '../../types/api';

interface ColumnsTableProps {
  columns: readonly Column[];
  onSaveColumn?: (columnName: string, description: string | null) => void;
}

export function ColumnsTable({ columns, onSaveColumn }: ColumnsTableProps) {
  const [editingCol, setEditingCol] = useState<string | null>(null);
  const [draft, setDraft] = useState('');

  const startEdit = (col: Column) => {
    setEditingCol(col.name);
    setDraft(col.description ?? '');
  };

  const saveEdit = (colName: string) => {
    onSaveColumn?.(colName, draft || null);
    setEditingCol(null);
  };

  const cancelEdit = () => setEditingCol(null);

  if (columns.length === 0) return null;

  return (
    <Table aria-label="Columns" className="mt-4">
      <TableHeader>
        <TableRow>
          <TableHeaderCell>Name</TableHeaderCell>
          <TableHeaderCell>Type</TableHeaderCell>
          <TableHeaderCell>Description</TableHeaderCell>
          <TableHeaderCell>Flags</TableHeaderCell>
          <TableHeaderCell>References</TableHeaderCell>
          {onSaveColumn && <TableHeaderCell>Actions</TableHeaderCell>}
        </TableRow>
      </TableHeader>
      <TableBody>
        {columns.map((col) => (
          <TableRow key={col.name}>
            <TableCell>
              <Text weight={col.isPrimaryKey ? 'bold' : 'regular'}>{col.name}</Text>
            </TableCell>
            <TableCell>
              <Text size={200}>
                {col.type}
                {col.maxLength != null ? `(${col.maxLength})` : ''}
              </Text>
            </TableCell>
            <TableCell>
              {editingCol === col.name ? (
                <div className="flex items-center gap-1">
                  <Input
                    value={draft}
                    onChange={(_e, data) => setDraft(data.value)}
                    size="small"
                    className="flex-1"
                  />
                  <Button
                    icon={<Checkmark24Regular />}
                    appearance="subtle"
                    size="small"
                    onClick={() => saveEdit(col.name)}
                  />
                  <Button
                    icon={<Dismiss24Regular />}
                    appearance="subtle"
                    size="small"
                    onClick={cancelEdit}
                  />
                </div>
              ) : (
                <Text size={200}>{col.description || '—'}</Text>
              )}
            </TableCell>
            <TableCell>
              <div className="flex gap-1 flex-wrap">
                {col.isPrimaryKey && <Badge size="small">PK</Badge>}
                {col.isNullable && <Badge size="small">Nullable</Badge>}
                {col.isIdentity && <Badge size="small">Identity</Badge>}
                {col.isComputed && <Badge size="small">Computed</Badge>}
              </div>
            </TableCell>
            <TableCell>
              {col.referencedTable && (
                <Text size={200}>
                  {col.referencedTable}.{col.referencedColumn}
                </Text>
              )}
            </TableCell>
            {onSaveColumn && (
              <TableCell>
                {editingCol !== col.name && (
                  <Button
                    icon={<Edit24Regular />}
                    appearance="subtle"
                    size="small"
                    onClick={() => startEdit(col)}
                    aria-label={`Edit ${col.name} description`}
                  />
                )}
              </TableCell>
            )}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
