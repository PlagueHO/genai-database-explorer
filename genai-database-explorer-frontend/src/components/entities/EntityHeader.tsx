import { Badge, Title2 } from '@fluentui/react-components';

interface EntityHeaderProps {
  schema: string;
  name: string;
  notUsed: boolean;
}

export function EntityHeader({ schema, name, notUsed }: EntityHeaderProps) {
  return (
    <div className="flex items-center gap-3 mb-4">
      <Title2>
        {schema}.{name}
      </Title2>
      {notUsed && (
        <Badge appearance="filled" color="warning">
          Not Used
        </Badge>
      )}
    </div>
  );
}
