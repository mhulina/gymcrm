import {gymSubscriptionTypeOptions} from "../utils/Mapper";
import {useState} from "react";

export function GymSubscriptionTypeDropdown({ userSubscriptionType }: { userSubscriptionType: number }) {
    const [selectedType, setSelectedType] = useState<number>(userSubscriptionType);

    return (
        <select
            value={selectedType}
            onChange={(e) => setSelectedType(Number(e.target.value))}
        >
            {gymSubscriptionTypeOptions.map((opt) => (
                <option key={opt.value} value={opt.value}>
                    {opt.label}
                </option>
            ))}
        </select>
    );
}