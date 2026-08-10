import {useState} from "react";
import {Button} from "../../../shared/components/Button";
import {TrainerDailyAvailability} from "../types/trainerDailyAvailability";
import {TrainerWorkingHours} from "../types/trainerWorkingHours";
import {addWorkingHoursToDailyAvailability, deleteWorkingHours, setDayOffStatus, updateWorkingHours} from "../api/schedulingApi";

interface Props {
    trainerId: string;
    dayName: string;
    day?: TrainerDailyAvailability;
    onAdded: () => void;
    // Weekends when the trainer's "working weekends" flag is off - automatically a day off,
    // not just editable-and-defaulted, so no toggle/hours UI is shown at all for it.
    locked?: boolean;
}

// One day's ranges (each editable/removable in place) plus its own "add a time range"
// mini-form and a day-off toggle, with its own submitting/error state - kept separate so
// the week editor stays readable.
export function DailyAvailabilityRow({ trainerId, dayName, day, onAdded, locked = false }: Props) {
    const [startTime, setStartTime] = useState("09:00");
    const [endTime, setEndTime] = useState("17:00");
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [showForm, setShowForm] = useState(false);

    const [editingId, setEditingId] = useState<string | null>(null);
    const [editStartTime, setEditStartTime] = useState("09:00");
    const [editEndTime, setEditEndTime] = useState("17:00");
    const [savingEdit, setSavingEdit] = useState(false);
    const [editError, setEditError] = useState<string | null>(null);

    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [deleteError, setDeleteError] = useState<string | null>(null);

    const [togglingDayOff, setTogglingDayOff] = useState(false);
    const [dayOffError, setDayOffError] = useState<string | null>(null);

    const isDayOff = day?.isDayOff ?? false;
    const hours = day?.workingHours ?? [];

    if (locked) {
        return (
            <div className="flex items-center justify-between border-b border-slate-100 dark:border-slate-800 py-3 last:border-0">
                <span className="text-sm font-medium text-slate-900 dark:text-white">{dayName}</span>
                <span className="text-xs text-slate-400 dark:text-slate-500">Day off (weekends not worked)</span>
            </div>
        );
    }

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

    function openEdit(h: TrainerWorkingHours) {
        setEditingId(h.id);
        setEditStartTime(h.startTime);
        setEditEndTime(h.endTime);
        setEditError(null);
    }

    async function handleSaveEdit(id: string) {
        if (editStartTime >= editEndTime) {
            setEditError("End time must be after start time.");
            return;
        }
        setSavingEdit(true);
        setEditError(null);

        const success = await updateWorkingHours(id, { startTime: editStartTime, endTime: editEndTime });

        if (success) {
            setEditingId(null);
            onAdded();
        } else {
            setEditError("Couldn't update that time range.");
        }

        setSavingEdit(false);
    }

    async function handleDelete(id: string) {
        setDeletingId(id);
        setDeleteError(null);

        const success = await deleteWorkingHours(id);

        if (success) {
            onAdded();
        } else {
            setDeleteError("Couldn't remove that time range.");
        }

        setDeletingId(null);
    }

    async function handleToggleDayOff(checked: boolean) {
        setTogglingDayOff(true);
        setDayOffError(null);

        const success = await setDayOffStatus(trainerId, dayName, checked);

        if (success) {
            onAdded();
        } else {
            setDayOffError("Couldn't update day-off status.");
        }

        setTogglingDayOff(false);
    }

    return (
        <div className="flex flex-col gap-2 border-b border-slate-100 dark:border-slate-800 py-3 last:border-0">
            <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-slate-900 dark:text-white">{dayName}</span>
                <label className="flex items-center gap-1.5 text-xs font-medium text-slate-500 dark:text-slate-400">
                    <input
                        type="checkbox"
                        checked={isDayOff}
                        disabled={togglingDayOff}
                        onChange={(e) => handleToggleDayOff(e.target.checked)}
                        className="rounded border-slate-300 dark:border-slate-700"
                    />
                    Day off
                </label>
            </div>
            {dayOffError && <p className="text-xs text-red-600 dark:text-red-400">{dayOffError}</p>}

            {!isDayOff && hours.length > 0 && (
                <ul className="flex flex-wrap gap-2">
                    {hours.map((h) =>
                        editingId === h.id ? (
                            <li
                                key={h.id}
                                className="flex flex-wrap items-end gap-2 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1"
                            >
                                <label className="text-xs text-slate-500 dark:text-slate-400">
                                    Start
                                    <input
                                        type="time"
                                        value={editStartTime}
                                        onChange={(e) => setEditStartTime(e.target.value)}
                                        className="mt-1 block rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-sm text-slate-900 dark:text-white"
                                    />
                                </label>
                                <label className="text-xs text-slate-500 dark:text-slate-400">
                                    End
                                    <input
                                        type="time"
                                        value={editEndTime}
                                        onChange={(e) => setEditEndTime(e.target.value)}
                                        className="mt-1 block rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-sm text-slate-900 dark:text-white"
                                    />
                                </label>
                                <Button type="button" variant="secondary" onClick={() => handleSaveEdit(h.id)} disabled={savingEdit}>
                                    {savingEdit ? "Saving..." : "Save"}
                                </Button>
                                <Button type="button" variant="ghost" onClick={() => setEditingId(null)}>
                                    Cancel
                                </Button>
                            </li>
                        ) : (
                            <li
                                key={h.id}
                                className="flex items-center gap-1 rounded-full bg-slate-100 dark:bg-slate-800 px-3 py-1 text-xs text-slate-600 dark:text-slate-300"
                            >
                                <button type="button" onClick={() => openEdit(h)} className="hover:underline">
                                    {h.startTime}–{h.endTime}
                                </button>
                                <button
                                    type="button"
                                    onClick={() => handleDelete(h.id)}
                                    disabled={deletingId === h.id}
                                    aria-label="Remove time range"
                                    className="text-slate-400 hover:text-red-600 dark:hover:text-red-400 disabled:opacity-50"
                                >
                                    ×
                                </button>
                            </li>
                        )
                    )}
                </ul>
            )}
            {editError && <p className="text-xs text-red-600 dark:text-red-400">{editError}</p>}
            {deleteError && <p className="text-xs text-red-600 dark:text-red-400">{deleteError}</p>}
            {!isDayOff && hours.length === 0 && (
                <p className="text-xs text-slate-400 dark:text-slate-500">No hours set yet.</p>
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
