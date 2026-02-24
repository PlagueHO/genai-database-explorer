import { Card, CardHeader, Text, Title3, Badge } from '@fluentui/react-components';
import { useProject } from '../hooks/useProject';
import { useModel } from '../hooks/useModel';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { ErrorBanner } from '../components/common/ErrorBanner';

export function DashboardPage() {
  const project = useProject();
  const model = useModel();

  if (project.isLoading || model.isLoading) {
    return <LoadingSpinner label="Loading dashboard..." />;
  }

  const error = project.error ?? model.error;
  if (error) {
    return <ErrorBanner error={error} />;
  }

  return (
    <div className="space-y-6">
      <Title3>Dashboard</Title3>

      {/* Project Info */}
      {project.data && (
        <Card className="p-4">
          <CardHeader header={<Text weight="semibold">Project</Text>} />
          <div className="grid grid-cols-2 gap-2 mt-2">
            <Text size={200} className="text-gray-600">
              Path
            </Text>
            <Text size={200}>{project.data.projectPath}</Text>
            <Text size={200} className="text-gray-600">
              Persistence Strategy
            </Text>
            <Text size={200}>{project.data.persistenceStrategy}</Text>
            <Text size={200} className="text-gray-600">
              Model Loaded
            </Text>
            <Text size={200}>
              <Badge
                appearance="filled"
                color={project.data.modelLoaded ? 'success' : 'warning'}
                size="small"
              >
                {project.data.modelLoaded ? 'Yes' : 'No'}
              </Badge>
            </Text>
          </div>
        </Card>
      )}

      {/* Model Summary */}
      {model.data && (
        <Card className="p-4">
          <CardHeader header={<Text weight="semibold">Semantic Model</Text>} />
          <div className="grid grid-cols-2 gap-2 mt-2">
            <Text size={200} className="text-gray-600">
              Name
            </Text>
            <Text size={200}>{model.data.name}</Text>
            <Text size={200} className="text-gray-600">
              Source
            </Text>
            <Text size={200}>{model.data.source}</Text>
            <Text size={200} className="text-gray-600">
              Description
            </Text>
            <Text size={200}>{model.data.description || '—'}</Text>
          </div>
          <div className="flex gap-6 mt-4">
            <div className="text-center">
              <Text size={600} weight="bold" className="block">
                {model.data.tableCount}
              </Text>
              <Text size={200} className="text-gray-600">
                Tables
              </Text>
            </div>
            <div className="text-center">
              <Text size={600} weight="bold" className="block">
                {model.data.viewCount}
              </Text>
              <Text size={200} className="text-gray-600">
                Views
              </Text>
            </div>
            <div className="text-center">
              <Text size={600} weight="bold" className="block">
                {model.data.storedProcedureCount}
              </Text>
              <Text size={200} className="text-gray-600">
                Stored Procedures
              </Text>
            </div>
          </div>
        </Card>
      )}
    </div>
  );
}
