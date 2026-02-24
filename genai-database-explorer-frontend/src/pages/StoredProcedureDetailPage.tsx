import { useParams, useNavigate } from 'react-router';
import { Title3 } from '@fluentui/react-components';
import { useStoredProcedureDetail, usePatchStoredProcedure } from '../hooks/useStoredProcedures';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { ErrorBanner } from '../components/common/ErrorBanner';
import { EntityHeader } from '../components/entities/EntityHeader';
import { EditableField } from '../components/common/EditableField';
import { NotUsedEditor } from '../components/common/NotUsedEditor';
import { ParametersDisplay } from '../components/entities/ParametersDisplay';
import { DefinitionViewer } from '../components/entities/DefinitionViewer';

export function StoredProcedureDetailPage() {
  const { schema, name } = useParams<{ schema: string; name: string }>();
  const navigate = useNavigate();
  const { data, isLoading, error } = useStoredProcedureDetail(schema!, name!);
  const patchSP = usePatchStoredProcedure(schema!, name!);

  if (isLoading) return <LoadingSpinner label="Loading stored procedure..." />;
  if (error?.message?.includes('Not Found')) {
    navigate('/stored-procedures', { replace: true });
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
        onSave={(v) => patchSP.mutate({ description: v })}
        multiline
      />
      <EditableField
        label="Semantic Description"
        value={data.semanticDescription}
        onSave={(v) => patchSP.mutate({ semanticDescription: v })}
        multiline
      />
      <NotUsedEditor
        notUsed={data.notUsed}
        notUsedReason={data.notUsedReason}
        onSave={(notUsed, notUsedReason) => patchSP.mutate({ notUsed, notUsedReason })}
      />

      {data.additionalInformation && (
        <div>
          <Title3>Additional Information</Title3>
          <p className="whitespace-pre-wrap text-sm">{data.additionalInformation}</p>
        </div>
      )}

      <ParametersDisplay parameters={data.parameters} />
      <DefinitionViewer definition={data.definition} />
    </div>
  );
}
