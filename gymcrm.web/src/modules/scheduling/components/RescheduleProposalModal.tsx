import {useState} from "react";
import {TrainingSession} from "../types/trainingSession";
import {acceptRescheduledTrainingSession, declineRescheduledTrainingSession} from "../api/schedulingApi";
import {Button} from "../../../shared/components/Button";

const DATE_FORMATTER = new Intl.DateTimeFormat(undefined, {weekday: "long", month: "long", day: "numeric"});
const TIME_FORMATTER = new Intl.DateTimeFormat(undefined, {hour: "2-digit", minute: "2-digit"});

interface Props {
    session: TrainingSession;
    start: Date;
    end: Date;
    onResolved: () => void;
}

// Blocking popup for a trainer-proposed reschedule (TrainingSessionStatus.Reschedule) - the
// session stays unconfirmed until the client explicitly accepts or declines it here, rather
// than the trainer's new time silently taking effect.
export function RescheduleProposalModal({session, start, end, onResolved}: Props) {
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function handleAccept() {
        setBusy(true);
        setError(null);
        const success = await acceptRescheduledTrainingSession(session.id);
        if (success) {
            onResolved();
        } else {
            setError("We couldn't confirm the new time. Try again.");
        }
        setBusy(false);
    }

    async function handleDecline() {
        setBusy(true);
        setError(null);
        const success = await declineRescheduledTrainingSession(session.id);
        if (success) {
            onResolved();
        } else {
            setError("We couldn't decline the new time. Try again.");
        }
        setBusy(false);
    }

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
            <div className="w-full max-w-sm rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-xl">
                <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
                    Reschedule proposed
                </h2>
                <p className="mt-2 text-sm text-slate-700 dark:text-slate-300">
                    Your trainer has proposed a new time for this session:
                </p>
                <p className="mt-3 text-base font-bold text-slate-900 dark:text-white">{DATE_FORMATTER.format(start)}</p>
                <p className="text-sm text-slate-600 dark:text-slate-400">
                    {TIME_FORMATTER.format(start)} – {TIME_FORMATTER.format(end)}
                </p>
                {session.description && (
                    <p className="mt-2 text-xs text-slate-500 dark:text-slate-400">{session.description}</p>
                )}

                {error && <p className="mt-3 text-xs text-red-600 dark:text-red-400">{error}</p>}

                <div className="mt-5 flex gap-2">
                    <Button type="button" onClick={handleAccept} disabled={busy}>
                        {busy ? "Saving..." : "Accept"}
                    </Button>
                    <Button type="button" variant="ghost" onClick={handleDecline} disabled={busy}>
                        Decline
                    </Button>
                </div>
            </div>
        </div>
    );
}
