import { Spinner } from '@fluentui/react-components';

interface LoadingSpinnerProps {
  label?: string;
}

export function LoadingSpinner({ label = 'Loading...' }: LoadingSpinnerProps) {
  return (
    <div className="flex items-center justify-center p-8">
      <Spinner label={label} />
    </div>
  );
}
