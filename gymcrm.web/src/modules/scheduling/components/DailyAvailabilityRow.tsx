import {useState} from "react";
import {Button} from "../../../shared/components/Button";
import {TrainerDailyAvailability} from "../types/trainerDailyAvailability";
import {addWorkingHoursToDailyAvailability} from "../api/schedulingApi";

interface Props {
    trainerId: string;
    dayName: string;
    day?: TrainerDailyAvailability;
    onAdded: () => void;
}

// One day's read-only ranges plus its own "add a time range" mini-form, with its
// own submitting/error state - kept separate so the week editor stays readable.
export function DailyAvailabilityRow({ trainerId, dayName, day, onAdded }: Props) {
    const [startTime, setStartTime] = useState("09:00");
    const [endTime, setEndTime] = useState("17:00");
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [showForm, setShowForm] = useState(false);

    const isDayOff = day?.isDayOff ?? false;
    const hours = day?.workingHours ?? [];

    async function handleAdd() {
        if (startTime >= endTime) {
            setError("End time must be after start time.");
            return;
        }
        setSubmitting(true);
        setError(null);

        const success = await addWorkingHoursToDailyAvailability(trainerId, dayName, [{ startTime, endTime }]);

        if (success) {
            setShowForm(false);
            onAdded();
        } else {
            setError("Couldn't add that time range.");
        }

        setSubmitting(false);
    }

    return (
        <div className="flex flex-col gap-2 border-b border-slate-100 dark:border-slate-800 py-3 last:border-0">
            <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-slate-900 dark:text-white">{dayName}</span>
                {isDayOff && (
                    <span className="text-xs font-semibold text-slate-400 dark:text-slate-500">Day off</span>
                )}
            </div>

            {hours.length > 0 ? (
                <ul className="flex flex-wrap gap-2">
                    {hours.map((h) => (
                        <li key={h.id} className="rounded-full bg-slate-100 dark:bg-slate-800 px-3 py-1 text-xs text-slate-600 dark:text-slate-300">
                            {h.startTime}–{h.endTime}
                        </li>
                    ))}
                </ul>
            ) : (
                !isDayOff && <p className="text-xs text-slate-400 dark:text-slate-500">No hours set yet.</p>
            )}

            {!isDayOff && (
                showForm ? (
                    <div className="flex flex-wrap items-end gap-2">
                        <label className="text-xs text-slate-500 dark:text-slate-400">
                            Start
                            <input
                                type="time"
                                value={startTime}
                                onChange={(e) => setStartTime(e.target.value)}
                                className="mt-1 block rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-sm text-slate-900 dark:text-white"
                            />
                        </label>
                        <label className="text-xs text-slate-500 dark:text-slate-400">
                            End
                            <input
                                type="time"
                                value={endTime}
                                onChange={(e) => setEndTime(e.target.value)}
                                className="mt-1 block rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-sm text-slate-900 dark:text-white"
                            />
                        </label>
                        <Button type="button" variant="secondary" onClick={handleAdd} disabled={submitting}>
                            {submitting ? "Adding..." : "Add"}
                        </Button>
                        <Button type="button" variant="ghost" onClick={() => setShowForm(false)}>
                            Cancel
                        </Button>
                    </div>
                ) : (
                    <button
                        type="button"
                        onClick={() => setShowForm(true)}
                        className="self-start text-xs font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300"
                    >
                        + Add time range
                    </button>
                )
            )}
            {error && <p className="text-xs text-red-600 dark:text-red-400">{error}</p>}
        </div>
    );
}
