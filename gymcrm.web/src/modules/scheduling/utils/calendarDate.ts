import {DAYS_OF_WEEK, DayOfWeekName} from "../constants/daysOfWeek";

// All helpers here work in the browser's local time and never touch UTC -
// Date.prototype.toISOString()/getUTC* would shift by the browser's offset,
// which this app's scheduling data (naive "HH:mm" working hours, naive
// booking timestamps) has no way to account for. See insertTrainingSession.ts.

function pad(value: number): string {
    return value.toString().padStart(2, "0");
}

export function toDateInputValue(date: Date): string {
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

export function toTimeInputValue(date: Date): string {
    return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function dayOfWeekName(date: Date): DayOfWeekName {
    return DAYS_OF_WEEK[date.getDay()];
}

export function isPastDate(date: Date): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const compare = new Date(date);
    compare.setHours(0, 0, 0, 0);
    return compare < today;
}

export function isSameDay(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

// dateValue: "YYYY-MM-DD" (from <input type="date">), timeValue: "HH:mm" (from <input type="time">).
export function buildLocalDateTime(dateValue: string, timeValue: string): string {
    return `${dateValue}T${timeValue}:00`;
}

// Parses a naive "YYYY-MM-DDTHH:mm:ss" string (as returned by the backend) into
// a Date using local time components, NOT `new Date(string)`, which for a
// string with no timezone offset is parsed inconsistently across engines
// (some treat it as UTC, some as local) - we always want local.
export function parseLocalDateTime(value: string): Date {
    const [datePart, timePart] = value.split("T");
    const [year, month, day] = datePart.split("-").map(Number);
    const [hours, minutes] = (timePart ?? "00:00").split(":").map(Number);
    return new Date(year, month - 1, day, hours, minutes);
}

export function addMinutesToTime(time: string, minutes: number): string {
    const [hours, mins] = time.split(":").map(Number);
    const total = hours * 60 + mins + minutes;
    const normalized = ((total % 1440) + 1440) % 1440;
    return `${pad(Math.floor(normalized / 60))}:${pad(normalized % 60)}`;
}

// Returns the UTC offset (in minutes) `timeZone` has at the instant `utcMs`. Standard
// Intl.DateTimeFormat-offset-lookup technique - accurate for any date since it re-derives
// the offset per instant (handles DST correctly outside the transition hour itself).
function getTimeZoneOffsetMinutes(utcMs: number, timeZone: string): number {
    const parts = new Intl.DateTimeFormat("en-US", {
        timeZone,
        hourCycle: "h23",
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
    }).formatToParts(new Date(utcMs));

    const value: Record<string, string> = {};
    parts.forEach((part) => {
        value[part.type] = part.value;
    });

    const asUtc = Date.UTC(
        Number(value.year),
        Number(value.month) - 1,
        Number(value.day),
        Number(value.hour),
        Number(value.minute),
        Number(value.second)
    );
    return (asUtc - utcMs) / 60000;
}

// Converts a naive "wall clock in fromZone" into the equivalent naive wall clock in
// toZone, for the given calendar date. This is how trainer-local session/working-hours
// times (the frame everything is stored/validated in - see insertTrainingSession.ts)
// get bridged to and from the booking member's own timezone for display/input.
//
// Single-pass offset resolution: correct for any date except within the ~1hr instant a
// DST transition itself occurs in `fromZone`, an accepted trade-off for a booking app.
export function convertWallClock(
    dateValue: string,
    timeValue: string,
    fromZone: string,
    toZone: string
): { date: string; time: string } {
    if (fromZone === toZone) {
        return { date: dateValue, time: timeValue };
    }

    const [year, month, day] = dateValue.split("-").map(Number);
    const [hour, minute] = timeValue.split(":").map(Number);
    const naiveAsUtc = Date.UTC(year, month - 1, day, hour, minute);

    const trueUtcMs = naiveAsUtc - getTimeZoneOffsetMinutes(naiveAsUtc, fromZone) * 60000;
    const targetMs = trueUtcMs + getTimeZoneOffsetMinutes(trueUtcMs, toZone) * 60000;

    const d = new Date(targetMs);
    return {
        date: `${d.getUTCFullYear()}-${pad(d.getUTCMonth() + 1)}-${pad(d.getUTCDate())}`,
        time: `${pad(d.getUTCHours())}:${pad(d.getUTCMinutes())}`,
    };
}
