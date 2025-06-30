import { AccountType } from "../Constants/Enums/AccountType";
import {GymSubscriptionType} from "../Constants/Enums/GymSubscriptionType";

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
