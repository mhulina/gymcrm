// Must match System.DayOfWeek's names exactly (backend validates against
// Enum.GetNames<DayOfWeek>()) and order (Sunday first, matching .NET's enum).
export const DAYS_OF_WEEK = [
    "Sunday",
    "Monday",
    "Tuesday",
    "Wednesday",
    "Thursday",
    "Friday",
    "Saturday",
] as const;

export type DayOfWeekName = typeof DAYS_OF_WEEK[number];
