import {useEffect, useState} from "react";
import {DAYS_OF_WEEK} from "../constants/daysOfWeek";
import {TrainerAvailability} from "../types/trainerAvailability";
import {fetchAvailabilitiesForTrainer} from "../api/schedulingApi";

interface Props {
    trainerId: string;
}

// Compact glance view of a trainer's own weekly hours - the full editable version lives
// at /member/edit (TrainerWeeklyScheduleEditor), this is read-only.
export function TrainerWorkingHoursSummary({trainerId}: Props) {
    const [availability, setAvailability] = useState<TrainerAvailability | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        setLoading(true);
        fetchAvailabilitiesForTrainer(trainerId)
            .then((list) => setAvailability(list[0] ?? null))
            .finally(() => setLoading(false));
    }, [trainerId]);

    const dailyByDayOfWeek = new Map(
        (availability?.dailyAvailabilities ?? []).map((daily) => [daily.dayOfWeek, daily])
    );

    return (
        <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-sm">
            <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
                Work hours
            </h2>

            {loading ? (
                <p className="mt-3 text-sm text-slate-500 dark:text-slate-400">Loading...</p>
            ) : (
                <ul className="mt-3 space-y-1.5 text-sm">
                    {DAYS_OF_WEEK.map((dayName) => {
                        const daily = dailyByDayOfWeek.get(dayName);
                        const hours = daily?.workingHours ?? [];
                        return (
                            <li key={dayName} className="flex justify-between gap-4">
                                <span className="text-slate-500 dark:text-slate-400">{dayName.slice(0, 3)}</span>
                                <span className="font-medium text-slate-800 dark:text-slate-100">
                                    {daily?.isDayOff
                                        ? "Day off"
                                        : hours.length > 0
                                            ? hours.map((h) => `${h.startTime}–${h.endTime}`).join(", ")
                                            : "Not set"}
                                </span>
                            </li>
                        );
                    })}
                </ul>
            )}
        </div>
    );
}
