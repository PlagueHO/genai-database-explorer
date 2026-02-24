import { SearchBox } from '@fluentui/react-components';

interface SearchInputProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

export function SearchInput({ value, onChange, placeholder = 'Search...' }: SearchInputProps) {
  return (
    <SearchBox
      value={value}
      onChange={(_e, data) => onChange(data.value)}
      placeholder={placeholder}
      className="w-full max-w-sm"
    />
  );
}
