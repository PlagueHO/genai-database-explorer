import { Text, Input } from '@fluentui/react-components';
import { useAppUI } from '../../context/AppUIContext';

export function ChatPanel() {
  const { chatPanelOpen } = useAppUI();

  if (!chatPanelOpen) return null;

  return (
    <div className="w-80 border-l h-full flex flex-col bg-white">
      <div className="p-3 border-b">
        <Text weight="semibold" size={400}>
          Chat
        </Text>
      </div>
      <div className="flex-1 flex items-center justify-center p-4 text-center">
        <Text className="text-gray-500">Agentic chat capabilities coming soon.</Text>
      </div>
      <div className="p-3 border-t">
        <Input disabled placeholder="Chat coming soon..." className="w-full" />
      </div>
    </div>
  );
}
