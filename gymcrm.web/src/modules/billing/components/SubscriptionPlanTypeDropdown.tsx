import {SelectField} from "../../../shared/components/SelectField";
import {enumLabel} from "../../../shared/utils/mapper";
import {SubscriptionPlanType} from "../types/subscriptionPlanType";

const options = Object.values(SubscriptionPlanType)
    .filter((value) => typeof value === "number")
    .map((value) => ({
        value: value as number,
        label: enumLabel(SubscriptionPlanType[value as SubscriptionPlanType])
    }));

interface Props {
    id?: string;
    label: string;
    value: number;
    onChange: (value: number) => void;
    disabled?: boolean;
}

export function SubscriptionPlanTypeDropdown({ id, label, value, onChange, disabled }: Props) {
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
