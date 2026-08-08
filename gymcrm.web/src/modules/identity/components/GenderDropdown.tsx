import {genderOptions} from "../../../shared/utils/mapper";
import {SelectField} from "../../../shared/components/SelectField";

interface Props {
    id?: string;
    label: string;
    value: number;
    onChange: (value: number) => void;
    disabled?: boolean;
}

export function GenderDropdown({ id, label, value, onChange, disabled }: Props) {
    return (
        <SelectField
            id={id}
            label={label}
            value={value}
            disabled={disabled}
            onChange={(e) => onChange(Number(e.target.value))}
        >
            {genderOptions.map((opt) => (
                <option key={opt.value} value={opt.value}>
                    {opt.label}
                </option>
            ))}
        </SelectField>
    );
}
