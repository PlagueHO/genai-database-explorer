import { Text, Label } from '@fluentui/react-components';

interface DefinitionViewerProps {
  definition: string;
}

export function DefinitionViewer({ definition }: DefinitionViewerProps) {
  if (!definition) return null;

  return (
    <div className="mt-4">
      <Label weight="semibold" className="mb-2 block">
        SQL Definition
      </Label>
      <pre className="p-4 bg-gray-50 rounded-md overflow-x-auto border">
        <Text size={200} font="monospace">
          {definition}
        </Text>
      </pre>
    </div>
  );
}
