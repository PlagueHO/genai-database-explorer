import { useState } from 'react';
import { Button, Input, Textarea, Text, Label } from '@fluentui/react-components';
import { Edit24Regular, Checkmark24Regular, Dismiss24Regular } from '@fluentui/react-icons';

interface EditableFieldProps {
  label: string;
  value: string | null;
  onSave: (value: string) => void;
  multiline?: boolean;
}

export function EditableField({ label, value, onSave, multiline = false }: EditableFieldProps) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(value ?? '');

  const handleSave = () => {
    onSave(draft);
    setEditing(false);
  };

  const handleCancel = () => {
    setDraft(value ?? '');
    setEditing(false);
  };

  if (!editing) {
    return (
      <div className="mb-3">
        <div className="flex items-center gap-2 mb-1">
          <Label weight="semibold">{label}</Label>
          <Button
            icon={<Edit24Regular />}
            appearance="subtle"
            size="small"
            onClick={() => setEditing(true)}
            aria-label={`Edit ${label}`}
          />
        </div>
        <Text className="whitespace-pre-wrap break-words max-h-48 overflow-y-auto block">
          {value || '—'}
        </Text>
      </div>
    );
  }

  return (
    <div className="mb-3">
      <Label weight="semibold" className="mb-1 block">
        {label}
      </Label>
      {multiline ? (
        <Textarea
          value={draft}
          onChange={(_e, data) => setDraft(data.value)}
          className="w-full"
          rows={4}
        />
      ) : (
        <Input value={draft} onChange={(_e, data) => setDraft(data.value)} className="w-full" />
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
