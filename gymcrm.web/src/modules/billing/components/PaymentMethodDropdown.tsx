import {SelectField} from "../../../shared/components/SelectField";
import {enumLabel} from "../../../shared/utils/mapper";
import {PaymentMethod} from "../types/paymentMethod";

const options = Object.values(PaymentMethod)
    .filter((value) => typeof value === "number")
    .map((value) => ({
        value: value as number,
        label: enumLabel(PaymentMethod[value as PaymentMethod])
    }));

interface Props {
    id?: string;
    label: string;
    value: number;
    onChange: (value: number) => void;
    disabled?: boolean;
}

export function PaymentMethodDropdown({ id, label, value, onChange, disabled }: Props) {
    return (
        <SelectField
            id={id}
            label={label}
            value={value}
            disabled={disabled}
            onChange={(e) => onChange(Number(e.target.value))}
        >
            {options.map((opt) => (
                <option key={opt.value} value={opt.value}>
                    {opt.label}
                </option>
            ))}
        </SelectField>
    );
}
