import {accountTypeOptions} from "../../../shared/utils/mapper";
import {useState} from "react";

export function AccountTypeDropdown({ userAccountType }: { userAccountType: number }) {
    const [selectedType, setSelectedType] = useState<number>(userAccountType);

    return (
        <select
            value={selectedType}
            onChange={(e) => setSelectedType(Number(e.target.value))}
        >
            {accountTypeOptions.map((opt) => (
                <option key={opt.value} value={opt.value}>
                    {opt.label}
                </option>
            ))}
        </select>
    );
}