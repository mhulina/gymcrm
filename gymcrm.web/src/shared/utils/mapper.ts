import { AccountType } from "../../modules/identity/types/accountType";
import {GymSubscriptionType} from "../../modules/identity/types/gymSubscriptionType";
import {Gender} from "../../modules/identity/types/gender";

// Numeric enums reverse-map to their PascalCase member name (e.g. "PersonalTrainer") -
// this turns that into a readable label (e.g. "Personal Trainer") for display anywhere
// in the UI, without needing a separate label lookup table per enum.
export function enumLabel(pascalCaseName: string): string {
    return pascalCaseName.replace(/([a-z0-9])([A-Z])/g, "$1 $2");
}

export const accountTypeOptions = Object.values(AccountType)
    .filter((value) => typeof value === "number")
    .map((value) => ({
        value: value as number,
        label: enumLabel(AccountType[value as AccountType])
    }));

export const gymSubscriptionTypeOptions = Object.values(GymSubscriptionType)
    .filter((value) => typeof value === "number")
    .map((value) => ({
        value: value as number,
        label: enumLabel(GymSubscriptionType[value as GymSubscriptionType])
    }));

export const genderOptions = Object.values(Gender)
    .filter((value) => typeof value === "number")
    .map((value) => ({
        value: value as number,
        label: enumLabel(Gender[value as Gender])
    }));
