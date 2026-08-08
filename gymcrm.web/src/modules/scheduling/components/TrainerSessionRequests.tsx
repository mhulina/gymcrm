import {useEffect, useMemo, useState} from "react";
import {Member} from "../../identity/types/member";
import {fetchAllMembers} from "../../identity/api/identityApi";
import {fullName} from "../../identity/utils/memberDisplay";
import {TrainingSession} from "../types/trainingSession";
import {TrainingSessionStatus} from "../types/trainingSessionStatus";
import {
    acceptTrainingSession,
    declineTrainingSession,
    fetchTrainingSessionsForTrainer,
    rescheduleTrainingSession,
} from "../api/schedulingApi";
import {addMinutesToTime, buildLocalDateTime, parseLocalDateTime, toDateInputValue, toTimeInputValue} from "../utils/calendarDate";
import {Button} from "../../../shared/components/Button";
import {Badge} from "../../../shared/components/Badge";

const DATE_FORMATTER = new Intl.DateTimeFormat(undefined, {weekday: "short", month: "short", day: "numeric"});
const TIME_FORMATTER = new Intl.DateTimeFormat(undefined, {hour: "2-digit", minute: "2-digit"});
const DURATIONS = [30, 60, 90] as const;

interface Props {
    trainerId: string;
}

// The actionable counterpart to TrainerSessionCalendar - shows only Requested sessions
// with Accept/Decline/Reschedule. Reschedule reuses DailyAvailabilityRow's inline-edit-mode
// idiom (toggle a small form in place) rather than a modal.
export function TrainerSessionRequests({trainerId}: Props) {
    const [sessions, setSessions] = useState<TrainingSession[]>([]);
    const [loading, setLoading] = useState(true);
    const [clients, setClients] = useState<Member[]>([]);
    const [actioningId, setActioningId] = useState<string | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);
    const [reschedulingId, setReschedulingId] = useState<string | null>(null);
    const [rescheduleDate, setRescheduleDate] = useState("");
    const [rescheduleStartTime, setRescheduleStartTime] = useState("09:00");
    const [rescheduleDuration, setRescheduleDuration] = useState<30 | 60 | 90>(60);
    const [reschedulingBusy, setReschedulingBusy] = useState(false);
    const [rescheduleError, setRescheduleError] = useState<string | null>(null);

    function reload() {
        setLoading(true);
        fetchTrainingSessionsForTrainer(trainerId)
            .then(setSessions)
            .finally(() => setLoading(false));
    }

    // eslint-disable-next-line react-hooks/exhaustive-deps
    useEffect(reload, [trainerId]);

    useEffect(() => {
        fetchAllMembers().then(setClients);
    }, []);

    const clientNameById = useMemo(() => {
        const map = new Map<string, string>();
        clients.forEach((client) => {
            if (client.accountGuid) map.set(client.accountGuid, fullName(client));
        });
        return map;
    }, [clients]);

    const requests = useMemo(
        () =>
            sessions
                .filter((session) => session.status === TrainingSessionStatus.Requested)
                .map((session) => ({
                    session,
                    start: parseLocalDateTime(session.startTime),
                    end: parseLocalDateTime(session.endTime),
                }))
                .sort((a, b) => a.start.getTime() - b.start.getTime()),
        [sessions]
    );

    async function handleAccept(id: string) {
        setActioningId(id);
        setActionError(null);

        const success = await acceptTrainingSession(id);
        if (success) {
            reload();
        } else {
            setActionError("Couldn't accept this request.");
        }
        setActioningId(null);
    }

    async function handleDecline(id: string) {
        setActioningId(id);
        setActionError(null);

        const success = await declineTrainingSession(id);
        if (success) {
            reload();
        } else {
            setActionError("Couldn't decline this request.");
        }
        setActioningId(null);
    }

    function openReschedule(session: TrainingSession, start: Date) {
        setReschedulingId(session.id);
        setRescheduleDate(toDateInputValue(start));
        setRescheduleStartTime(toTimeInputValue(start));
        setRescheduleDuration(60);
        setRescheduleError(null);
    }

    async function handleSaveReschedule(id: string) {
        setReschedulingBusy(true);
        setRescheduleError(null);

        const endTime = addMinutesToTime(rescheduleStartTime, rescheduleDuration);
        const result = await rescheduleTrainingSession(id, {
            newStartTime: buildLocalDateTime(rescheduleDate, rescheduleStartTime),
            newEndTime: buildLocalDateTime(rescheduleDate, endTime),
        });

        if (result.success) {
            setReschedulingId(null);
            reload();
        } else {
            setRescheduleError(result.error ?? "We couldn't reschedule this session.");
        }
        setReschedulingBusy(false);
    }

    return (
        <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-sm">
            <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
                Session requests
            </h2>

            {actionError && <p className="mt-3 text-sm text-red-600 dark:text-red-400">{actionError}</p>}

            {loading ? (
                <p className="mt-3 text-sm text-slate-500 dark:text-slate-400">Loading...</p>
            ) : requests.length === 0 ? (
                <p className="mt-3 text-sm text-slate-500 dark:text-slate-400">No pending session requests.</p>
            ) : (
                <ul className="mt-3 space-y-3">
                    {requests.map(({session, start, end}) => (
                        <li key={session.id} className="rounded-xl border border-slate-200 dark:border-slate-800 p-4">
                            <div className="flex flex-wrap items-center justify-between gap-2">
                                <div>
                                    <p className="text-sm font-semibold text-slate-900 dark:text-white">
                                        {clientNameById.get(session.clientId) ?? "Unknown client"}
                                    </p>
                                    <p className="text-xs text-slate-500 dark:text-slate-400">
                                        {DATE_FORMATTER.format(start)} · {TIME_FORMATTER.format(start)} – {TIME_FORMATTER.format(end)}
                                    </p>
                                    {session.description && (
                                        <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{session.description}</p>
                                    )}
                                </div>
                                <Badge tone="slate">Requested</Badge>
                            </div>

                            {reschedulingId === session.id ? (
                                <div className="mt-3 flex flex-wrap items-end gap-2 border-t border-slate-100 dark:border-slate-800 pt-3">
                                    <label className="text-xs text-slate-500 dark:text-slate-400">
                                        Date
                                        <input
                                            type="date"
                                            value={rescheduleDate}
                                            onChange={(e) => setRescheduleDate(e.target.value)}
                                            className="mt-1 block rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-sm text-slate-900 dark:text-white"
                                        />
                                    </label>
                                    <label className="text-xs text-slate-500 dark:text-slate-400">
                                        Start
                                        <input
                                            type="time"
                                            value={rescheduleStartTime}
                                            onChange={(e) => setRescheduleStartTime(e.target.value)}
                                            className="mt-1 block rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-sm text-slate-900 dark:text-white"
                                        />
                                    </label>
                                    <div className="flex gap-1">
                                        {DURATIONS.map((minutes) => (
                                            <button
                                                key={minutes}
                                                type="button"
                                                onClick={() => setRescheduleDuration(minutes)}
                                                className={`rounded-lg border px-2 py-1 text-xs font-semibold transition-colors ${
                                                    rescheduleDuration === minutes
                                                        ? "border-emerald-500 bg-emerald-600 text-white"
                                                        : "border-slate-300 dark:border-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800"
                                                }`}
                                            >
                                                {minutes}m
                                            </button>
                                        ))}
                                    </div>
                                    <Button
                                        type="button"
                                        variant="secondary"
                                        onClick={() => handleSaveReschedule(session.id)}
                                        disabled={reschedulingBusy}
                                    >
                                        {reschedulingBusy ? "Saving..." : "Save"}
                                    </Button>
                                    <Button type="button" variant="ghost" onClick={() => setReschedulingId(null)}>
                                        Cancel
                                    </Button>
                                    {rescheduleError && (
                                        <p className="w-full text-xs text-red-600 dark:text-red-400">{rescheduleError}</p>
                                    )}
                                </div>
                            ) : (
                                <div className="mt-3 flex gap-2 border-t border-slate-100 dark:border-slate-800 pt-3">
                                    <Button type="button" onClick={() => handleAccept(session.id)} disabled={actioningId === session.id}>
                                        Accept
                                    </Button>
                                    <Button
                                        type="button"
                                        variant="secondary"
                                        onClick={() => openReschedule(session, start)}
                                        disabled={actioningId === session.id}
                                    >
                                        Reschedule
                                    </Button>
                                    <Button
                                        type="button"
                                        variant="ghost"
                                        onClick={() => handleDecline(session.id)}
                                        disabled={actioningId === session.id}
                                    >
                                        Decline
                                    </Button>
                                </div>
                            )}
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
