import { Text } from '@fluentui/react-components';

interface EmptyStateProps {
  message: string;
}

export function EmptyState({ message }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center p-12 text-center">
      <Text size={400} className="text-gray-500">
        {message}
      </Text>
    </div>
  );
}
