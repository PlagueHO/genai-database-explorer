import { Text, Label } from '@fluentui/react-components';

interface ParametersDisplayProps {
  parameters: string | null;
}

export function ParametersDisplay({ parameters }: ParametersDisplayProps) {
  if (!parameters) return null;

  return (
    <div className="mt-4">
      <Label weight="semibold" className="mb-2 block">
        Parameters
      </Label>
      <pre className="p-4 bg-gray-50 rounded-md overflow-x-auto border">
        <Text size={200} font="monospace">
          {parameters}
        </Text>
      </pre>
    </div>
  );
}
