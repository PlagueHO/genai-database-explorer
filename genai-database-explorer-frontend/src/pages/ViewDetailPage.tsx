import { useParams, useNavigate } from 'react-router';
import { Title3 } from '@fluentui/react-components';
import { useViewDetail, usePatchView, usePatchViewColumn } from '../hooks/useViews';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { ErrorBanner } from '../components/common/ErrorBanner';
import { EntityHeader } from '../components/entities/EntityHeader';
import { EditableField } from '../components/common/EditableField';
import { NotUsedEditor } from '../components/common/NotUsedEditor';
import { ColumnsTable } from '../components/entities/ColumnsTable';
import { DefinitionViewer } from '../components/entities/DefinitionViewer';

export function ViewDetailPage() {
  const { schema, name } = useParams<{ schema: string; name: string }>();
  const navigate = useNavigate();
  const { data, isLoading, error } = useViewDetail(schema!, name!);
  const patchView = usePatchView(schema!, name!);
  const patchColumn = usePatchViewColumn(schema!, name!);

  if (isLoading) return <LoadingSpinner label="Loading view..." />;
  if (error?.message?.includes('Not Found')) {
    navigate('/views', { replace: true });
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
        onSave={(v) => patchView.mutate({ description: v })}
        multiline
      />
      <EditableField
        label="Semantic Description"
        value={data.semanticDescription}
        onSave={(v) => patchView.mutate({ semanticDescription: v })}
        multiline
      />
      <NotUsedEditor
        notUsed={data.notUsed}
        notUsedReason={data.notUsedReason}
        onSave={(notUsed, notUsedReason) => patchView.mutate({ notUsed, notUsedReason })}
      />

      <Title3>Columns</Title3>
      <ColumnsTable
        columns={data.columns}
        onSaveColumn={(colName, description) =>
          patchColumn.mutate({ columnName: colName, body: { description } })
        }
      />

      <DefinitionViewer definition={data.definition} />
    </div>
  );
}
