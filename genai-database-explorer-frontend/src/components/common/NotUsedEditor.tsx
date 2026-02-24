import { useState } from 'react';
import { Switch, Input, Button, Label } from '@fluentui/react-components';
import { Checkmark24Regular, Dismiss24Regular } from '@fluentui/react-icons';

interface NotUsedEditorProps {
  notUsed: boolean;
  notUsedReason: string | null;
  onSave: (notUsed: boolean, notUsedReason: string | null) => void;
}

export function NotUsedEditor({ notUsed, notUsedReason, onSave }: NotUsedEditorProps) {
  const [editing, setEditing] = useState(false);
  const [draftNotUsed, setDraftNotUsed] = useState(notUsed);
  const [draftReason, setDraftReason] = useState(notUsedReason ?? '');

  const handleSave = () => {
    onSave(draftNotUsed, draftNotUsed ? draftReason || null : null);
    setEditing(false);
  };

  const handleCancel = () => {
    setDraftNotUsed(notUsed);
    setDraftReason(notUsedReason ?? '');
    setEditing(false);
  };

  if (!editing) {
    return (
      <div className="mb-3">
        <Label weight="semibold" className="mr-2">
          Not Used:
        </Label>
        <span
          className="cursor-pointer underline"
          onClick={() => setEditing(true)}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => e.key === 'Enter' && setEditing(true)}
        >
          {notUsed ? `Yes — ${notUsedReason || 'No reason'}` : 'No'}
        </span>
      </div>
    );
  }

  return (
    <div className="mb-3">
      <Switch
        label="Not Used"
        checked={draftNotUsed}
        onChange={(_e, data) => setDraftNotUsed(data.checked)}
      />
      {draftNotUsed && (
        <Input
          value={draftReason}
          onChange={(_e, data) => setDraftReason(data.value)}
          placeholder="Reason..."
          className="w-full mt-1"
        />
      )}
      <div className="flex gap-1 mt-1">
        <Button
          icon={<Checkmark24Regular />}
          appearance="primary"
          size="small"
          onClick={handleSave}
        >
          Save
        </Button>
        <Button icon={<Dismiss24Regular />} appearance="subtle" size="small" onClick={handleCancel}>
          Cancel
        </Button>
      </div>
    </div>
  );
}
