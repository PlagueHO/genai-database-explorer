import { MessageBar, MessageBarBody, MessageBarTitle } from '@fluentui/react-components';

interface ErrorBannerProps {
  error: Error | null;
  onDismiss?: () => void;
}

export function ErrorBanner({ error, onDismiss }: ErrorBannerProps) {
  if (!error) return null;

  return (
    <MessageBar intent="error" className="mb-4" {...(onDismiss ? { onDismiss } : {})}>
      <MessageBarBody>
        <MessageBarTitle>Error</MessageBarTitle>
        {error.message}
      </MessageBarBody>
    </MessageBar>
  );
}
