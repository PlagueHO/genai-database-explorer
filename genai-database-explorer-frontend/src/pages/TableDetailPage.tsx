import { useParams, useNavigate } from 'react-router';
import { Title3 } from '@fluentui/react-components';
import { useTableDetail, usePatchTable, usePatchTableColumn } from '../hooks/useTables';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { ErrorBanner } from '../components/common/ErrorBanner';
import { EntityHeader } from '../components/entities/EntityHeader';
import { EditableField } from '../components/common/EditableField';
import { NotUsedEditor } from '../components/common/NotUsedEditor';
import { ColumnsTable } from '../components/entities/ColumnsTable';
import { IndexesTable } from '../components/entities/IndexesTable';

export function TableDetailPage() {
  const { schema, name } = useParams<{ schema: string; name: string }>();
  const navigate = useNavigate();
  const { data, isLoading, error } = useTableDetail(schema!, name!);
  const patchTable = usePatchTable(schema!, name!);
  const patchColumn = usePatchTableColumn(schema!, name!);

  if (isLoading) return <LoadingSpinner label="Loading table..." />;
  if (error?.message?.includes('Not Found')) {
    navigate('/tables', { replace: true });
    return null;
  }
  if (error) return <ErrorBanner error={error} />;
  if (!data) return null;

  return (
    <div className="space-y-4">
      <EntityHeader schema={data.schema} name={data.name} notUsed={data.notUsed} />

      <EditableField
        label="Description"
        value={data.description}
        onSave={(v) => patchTable.mutate({ description: v })}
        multiline
      />
      <EditableField
        label="Semantic Description"
        value={data.semanticDescription}
        onSave={(v) => patchTable.mutate({ semanticDescription: v })}
        multiline
      />
      <NotUsedEditor
        notUsed={data.notUsed}
        notUsedReason={data.notUsedReason}
        onSave={(notUsed, notUsedReason) => patchTable.mutate({ notUsed, notUsedReason })}
      />

      {data.additionalInformation && (
        <div>
          <Title3>Additional Information</Title3>
          <p className="whitespace-pre-wrap text-sm">{data.additionalInformation}</p>
        </div>
      )}

      <Title3>Columns</Title3>
      <ColumnsTable
        columns={data.columns}
        onSaveColumn={(colName, description) =>
          patchColumn.mutate({ columnName: colName, body: { description } })
        }
      />

      <Title3>Indexes</Title3>
      <IndexesTable indexes={data.indexes} />
    </div>
  );
}
