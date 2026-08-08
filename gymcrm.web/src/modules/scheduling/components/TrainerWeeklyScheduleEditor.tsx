import {useEffect, useState} from "react";
import {Button} from "../../../shared/components/Button";
import {Banner} from "../../../shared/components/Banner";
import {DAYS_OF_WEEK} from "../constants/daysOfWeek";
import {TrainerAvailability} from "../types/trainerAvailability";
import {addAvailability, fetchAvailabilitiesForTrainer, updateAvailability} from "../api/schedulingApi";
import {DailyAvailabilityRow} from "./DailyAvailabilityRow";

interface DaySetup {
    isDayOff: boolean;
    startTime: string;
    endTime: string;
}

function initialWeekSetup(): Record<string, DaySetup> {
    const setup: Record<string, DaySetup> = {};
    for (const day of DAYS_OF_WEEK) {
        setup[day] = { isDayOff: day === "Saturday" || day === "Sunday", startTime: "09:00", endTime: "17:00" };
    }
    return setup;
}

export function TrainerWeeklyScheduleEditor({ trainerId }: { trainerId: string }) {
    const [loading, setLoading] = useState(true);
    const [availabilities, setAvailabilities] = useState<TrainerAvailability[]>([]);
    const [weekSetup, setWeekSetup] = useState<Record<string, DaySetup>>(initialWeekSetup);
    const [workingWeekends, setWorkingWeekends] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [weekendsSaving, setWeekendsSaving] = useState(false);

    const reload = () => {
        setLoading(true);
        fetchAvailabilitiesForTrainer(trainerId)
            .then(setAvailabilities)
            .finally(() => setLoading(false));
    };

    useEffect(reload, [trainerId]);

    async function handleFirstTimeSetup(event: { preventDefault: () => void }) {
        event.preventDefault();
        setSubmitting(true);
        setError(null);

        const success = await addAvailability({
            trainerId,
            workingWeekends,
            dailyAvailabilities: DAYS_OF_WEEK.map((day) => ({
                dayOfWeek: day,
                isDayOff: weekSetup[day].isDayOff,
                workingHours: weekSetup[day].isDayOff
                    ? []
                    : [{ startTime: weekSetup[day].startTime, endTime: weekSetup[day].endTime }],
            })),
        });

        if (success) {
            reload();
        } else {
            setError("We couldn't set up the weekly schedule. Try again.");
        }

        setSubmitting(false);
    }

    async function handleToggleWeekends(availability: TrainerAvailability, checked: boolean) {
        setWeekendsSaving(true);
        const success = await updateAvailability({ ...availability, workingWeekends: checked });
        if (success) {
            reload();
        }
        setWeekendsSaving(false);
    }

    function updateDay(day: string, changes: Partial<DaySetup>) {
        setWeekSetup((prev) => ({ ...prev, [day]: { ...prev[day], ...changes } }));
    }

    if (loading) {
        return <p className="text-sm text-slate-500 dark:text-slate-400">Loading schedule...</p>;
    }

    if (availabilities.length === 0) {
        return (
            <form onSubmit={handleFirstTimeSetup} className="space-y-4">
                <p className="text-sm text-slate-500 dark:text-slate-400">
                    No weekly schedule set up yet. Pick working days and hours to get started.
                </p>
                {error && <Banner variant="error">{error}</Banner>}

                <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
                    <input
                        type="checkbox"
                        checked={workingWeekends}
                        onChange={(e) => setWorkingWeekends(e.target.checked)}
                        className="h-4 w-4 rounded border-slate-300 dark:border-slate-700 text-emerald-600 focus:ring-emerald-500"
                    />
                    Working weekends
                </label>

                <div className="divide-y divide-slate-100 dark:divide-slate-800">
                    {DAYS_OF_WEEK.map((day) => (
                        <div key={day} className="flex flex-wrap items-center gap-3 py-2.5">
                            <span className="w-24 text-sm font-medium text-slate-900 dark:text-white">{day}</span>
                            <label className="flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400">
                                <input
                                    type="checkbox"
                                    checked={weekSetup[day].isDayOff}
                                    onChange={(e) => updateDay(day, { isDayOff: e.target.checked })}
                                    className="h-3.5 w-3.5 rounded border-slate-300 dark:border-slate-700 text-emerald-600 focus:ring-emerald-500"
                                />
                                Day off
                            </label>
                            {!weekSetup[day].isDayOff && (
                                <>
                                    <input
                                        type="time"
                                        value={weekSetup[day].startTime}
                                        onChange={(e) => updateDay(day, { startTime: e.target.value })}
                                        className="rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-sm text-slate-900 dark:text-white"
                                    />
                                    <span className="text-xs text-slate-400">to</span>
                                    <input
                                        type="time"
                                        value={weekSetup[day].endTime}
                                        onChange={(e) => updateDay(day, { endTime: e.target.value })}
                                        className="rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-sm text-slate-900 dark:text-white"
                                    />
                                </>
                            )}
                        </div>
                    ))}
                </div>

                <Button type="submit" disabled={submitting}>
                    {submitting ? "Setting up..." : "Set up schedule"}
                </Button>
            </form>
        );
    }

    const availability = availabilities[0];

    return (
        <div className="space-y-4">
            {availabilities.length > 1 && (
                <Banner variant="info">
                    This trainer has more than one schedule on file. Showing the first one.
                </Banner>
            )}
            <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
                <input
                    type="checkbox"
                    checked={availability.workingWeekends}
                    disabled={weekendsSaving}
                    onChange={(e) => handleToggleWeekends(availability, e.target.checked)}
                    className="h-4 w-4 rounded border-slate-300 dark:border-slate-700 text-emerald-600 focus:ring-emerald-500"
                />
                Working weekends
            </label>

            <div className="divide-y divide-slate-100 dark:divide-slate-800">
                {DAYS_OF_WEEK.map((day) => (
                    <DailyAvailabilityRow
                        key={day}
                        trainerId={trainerId}
                        dayName={day}
                        day={(availability.dailyAvailabilities ?? []).find((d) => d.dayOfWeek === day)}
                        onAdded={reload}
                    />
                ))}
            </div>
        </div>
    );
}
