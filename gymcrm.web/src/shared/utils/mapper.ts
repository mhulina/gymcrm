import { AccountType } from "../../modules/identity/types/accountType";
import {GymSubscriptionType} from "../../modules/identity/types/gymSubscriptionType";
import {Gender} from "../../modules/identity/types/gender";

export const accountTypeOptions = Object.values(AccountType)
    .filter((value) => typeof value === "number")
    .map((value) => ({
        value: value as number,
        label: AccountType[value as AccountType]
    }));

export const gymSubscriptionTypeOptions = Object.values(GymSubscriptionType)
    .filter((value) => typeof value === "number")
    .map((value) => ({
        value: value as number,
        label: GymSubscriptionType[value as GymSubscriptionType]
    }));

export const genderOptions = Object.values(Gender)
    .filter((value) => typeof value === "number")
    .map((value) => ({
        value: value as number,
        label: Gender[value as Gender]
    }));
