import {useEffect, useMemo, useState} from "react";
import {Member} from "../../identity/types/member";
import {fullName, initials} from "../../identity/utils/memberDisplay";
import {TrainerAvailability} from "../types/trainerAvailability";
import {InsertTrainingSession} from "../types/insertTrainingSession";
import {addTrainingSession, fetchAvailabilitiesForTrainer} from "../api/schedulingApi";
import {
    addMinutesToTime,
    buildLocalDateTime,
    convertWallClock,
    dayOfWeekName,
    isPastDate,
    toDateInputValue,
} from "../utils/calendarDate";
import {Button} from "../../../shared/components/Button";
import {Banner} from "../../../shared/components/Banner";
import {TextField} from "../../../shared/components/TextField";

const STEP_LABELS = ["Select Trainer", "Choose Date & Time", "Send Request"] as const;
const DURATIONS = [30, 60, 90] as const;

interface Props {
    member: Member;
    trainers: Member[];
    trainersLoading: boolean;
    initialDate: Date;
    onClose: () => void;
    onBooked: () => void;
}

function parseDateInputValue(value: string): Date {
    const [year, month, day] = value.split("-").map(Number);
    return new Date(year, month - 1, day);
}

export function BookingWizard({member, trainers, trainersLoading, initialDate, onClose, onBooked}: Props) {
    const [step, setStep] = useState<1 | 2 | 3>(1);
    // Only pre-select the member's assigned trainer if they're actually in the (already
    // bookable-hours-filtered) trainers list - otherwise steps 2-3 would resolve
    // selectedTrainer to undefined and silently break.
    const [selectedTrainerId, setSelectedTrainerId] = useState(
        trainers.some((t) => t.accountGuid === member.personalTrainerId) ? member.personalTrainerId ?? "" : ""
    );
    const [trainerAvailability, setTrainerAvailability] = useState<TrainerAvailability | null>(null);
    const [date, setDate] = useState(toDateInputValue(initialDate));
    const [duration, setDuration] = useState<30 | 60 | 90>(60);
    const [startTime, setStartTime] = useState("09:00");
    const [description, setDescription] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (!selectedTrainerId) {
            setTrainerAvailability(null);
            return;
        }
        fetchAvailabilitiesForTrainer(selectedTrainerId).then((list) => setTrainerAvailability(list[0] ?? null));
    }, [selectedTrainerId]);

    const memberZone = member.timeZone;
    const selectedTrainer = trainers.find((t) => t.accountGuid === selectedTrainerId);
    const trainerZone = selectedTrainer?.timeZone;
    const selectedDate = useMemo(() => parseDateInputValue(date), [date]);
    const endTime = addMinutesToTime(startTime, duration);

    // Trainer's working hours/day-off are naive values in the TRAINER's own timezone
    // (see insertTrainingSession.ts) - the member picks date/time in their own zone, so
    // everything read from trainerAvailability has to be translated through the
    // trainer's local date/time first, then bounds shown back to the member get
    // translated back. Falls back to no conversion until a trainer/zone is known.
    const trainerLocalDate = trainerZone ? convertWallClock(date, startTime, memberZone, trainerZone).date : date;
    const dailyForSelectedDate = (trainerAvailability?.dailyAvailabilities ?? []).find(
        (daily) => daily.dayOfWeek === dayOfWeekName(parseDateInputValue(trainerLocalDate))
    );
    const isDayOff = dailyForSelectedDate?.isDayOff ?? false;
    const workingHours = dailyForSelectedDate?.workingHours ?? [];
    const minTimeTrainerLocal = workingHours.length > 0 ? workingHours.map((wh) => wh.startTime).sort()[0] : undefined;
    const maxTimeTrainerLocal =
        workingHours.length > 0 ? workingHours.map((wh) => wh.endTime).sort().slice(-1)[0] : undefined;
    // These bounds are a soft UX hint (the server has final say) - not worth handling the
    // rare case where converting a near-midnight bound would actually land on a different
    // member-local calendar date than the one currently selected.
    const minTime =
        trainerZone && minTimeTrainerLocal
            ? convertWallClock(trainerLocalDate, minTimeTrainerLocal, trainerZone, memberZone).time
            : minTimeTrainerLocal;
    const maxTime =
        trainerZone && maxTimeTrainerLocal
            ? convertWallClock(trainerLocalDate, maxTimeTrainerLocal, trainerZone, memberZone).time
            : maxTimeTrainerLocal;

    function handleClose() {
        onClose();
    }

    async function handleConfirm() {
        if (!member.accountGuid || !selectedTrainerId || !trainerZone) return;
        setSubmitting(true);
        setError(null);

        // Convert start and end independently (not by reusing one converted date) so a
        // session that crosses midnight in one zone but not the other still round-trips.
        const startTrainerLocal = convertWallClock(date, startTime, memberZone, trainerZone);
        const endTrainerLocal = convertWallClock(date, endTime, memberZone, trainerZone);

        const insert: InsertTrainingSession = {
            trainerId: selectedTrainerId,
            clientId: member.accountGuid,
            startTime: buildLocalDateTime(startTrainerLocal.date, startTrainerLocal.time),
            endTime: buildLocalDateTime(endTrainerLocal.date, endTrainerLocal.time),
            description: description || undefined,
        };

        const result = await addTrainingSession(insert);
        if (result.success) {
            onBooked();
        } else {
            setError(result.error ?? "We couldn't book this session.");
        }
        setSubmitting(false);
    }

    return (
        <div className="mt-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/40 p-4">
            <div className="flex justify-between text-xs font-bold text-slate-400 dark:text-slate-500 border-b border-slate-200 dark:border-slate-800 pb-3 mb-4">
                {STEP_LABELS.map((label, index) => {
                    const stepNumber = (index + 1) as 1 | 2 | 3;
                    const reached = stepNumber <= step;
                    return (
                        <span key={label} className={reached ? "text-emerald-600 dark:text-emerald-400" : ""}>
                            {stepNumber}. {label}
                        </span>
                    );
                })}
            </div>

            {step === 1 && (
                <div className="space-y-4">
                    {trainersLoading ? (
                        <p className="text-sm text-slate-500 dark:text-slate-400">Loading trainers...</p>
                    ) : trainers.length === 0 ? (
                        <p className="text-sm text-slate-500 dark:text-slate-400">No trainers are available to book with yet.</p>
                    ) : (
                        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                            {trainers.map((trainer) => {
                                const selected = trainer.accountGuid === selectedTrainerId;
                                return (
                                    <button
                                        key={trainer.accountGuid}
                                        type="button"
                                        onClick={() => setSelectedTrainerId(trainer.accountGuid ?? "")}
                                        className={`rounded-2xl border p-4 text-center transition-colors ${
                                            selected
                                                ? "border-emerald-500 bg-emerald-50 dark:bg-emerald-950/40"
                                                : "border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 hover:border-emerald-300"
                                        }`}
                                    >
                                        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-emerald-600 text-sm font-bold text-white">
                                            {initials(trainer)}
                                        </div>
                                        <p className="mt-2 text-sm font-bold text-slate-800 dark:text-slate-100">{fullName(trainer)}</p>
                                        {trainer.workingExperienceInMonths ? (
                                            <p className="text-xs text-slate-500 dark:text-slate-400">
                                                {trainer.workingExperienceInMonths} months experience
                                            </p>
                                        ) : null}
                                        {trainer.accountGuid === member.personalTrainerId && (
                                            <span className="mt-1 inline-block rounded-full bg-emerald-100 dark:bg-emerald-900/60 px-2 py-0.5 text-[10px] font-bold text-emerald-700 dark:text-emerald-300">
                                                Your trainer
                                            </span>
                                        )}
                                    </button>
                                );
                            })}
                        </div>
                    )}

                    <div className="flex justify-end gap-2">
                        <Button type="button" variant="ghost" onClick={handleClose}>Cancel</Button>
                        <Button type="button" disabled={!selectedTrainerId} onClick={() => setStep(2)}>Next</Button>
                    </div>
                </div>
            )}

            {step === 2 && (
                <div className="space-y-4">
                    {isDayOff && (
                        <Banner variant="info">
                            {selectedTrainer ? fullName(selectedTrainer) : "This trainer"} usually has this day off. You can still request it.
                        </Banner>
                    )}

                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <TextField
                            id="bookingDate"
                            label="Date"
                            type="date"
                            min={toDateInputValue(new Date())}
                            value={date}
                            onChange={(e) => setDate(e.target.value)}
                        />
                        <TextField
                            id="bookingStartTime"
                            label="Start time"
                            type="time"
                            min={minTime}
                            max={maxTime}
                            value={startTime}
                            onChange={(e) => setStartTime(e.target.value)}
                        />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1.5">Duration</label>
                        <div className="flex gap-2">
                            {DURATIONS.map((minutes) => (
                                <button
                                    key={minutes}
                                    type="button"
                                    onClick={() => setDuration(minutes)}
                                    className={`flex-1 rounded-lg border px-3 py-2 text-sm font-semibold transition-colors ${
                                        duration === minutes
                                            ? "border-emerald-500 bg-emerald-600 text-white"
                                            : "border-slate-300 dark:border-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800"
                                    }`}
                                >
                                    {minutes} min
                                </button>
                            ))}
                        </div>
                    </div>

                    <div className="flex justify-end gap-2">
                        <Button type="button" variant="ghost" onClick={() => setStep(1)}>Back</Button>
                        <Button type="button" disabled={isPastDate(selectedDate) || !startTime} onClick={() => setStep(3)}>
                            Next
                        </Button>
                    </div>
                </div>
            )}

            {step === 3 && (
                <div className="space-y-4">
                    {error && <Banner variant="error">{error}</Banner>}

                    <dl className="space-y-1.5 text-sm">
                        <div className="flex justify-between">
                            <dt className="text-slate-500 dark:text-slate-400">Trainer</dt>
                            <dd className="font-semibold text-slate-800 dark:text-slate-100">
                                {selectedTrainer ? fullName(selectedTrainer) : "—"}
                            </dd>
                        </div>
                        <div className="flex justify-between">
                            <dt className="text-slate-500 dark:text-slate-400">Date</dt>
                            <dd className="font-semibold text-slate-800 dark:text-slate-100">{date}</dd>
                        </div>
                        <div className="flex justify-between">
                            <dt className="text-slate-500 dark:text-slate-400">Time</dt>
                            <dd className="font-semibold text-slate-800 dark:text-slate-100">
                                {startTime} – {endTime} ({duration} min)
                            </dd>
                        </div>
                    </dl>

                    <div>
                        <label htmlFor="bookingDescription" className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1.5">
                            Notes (optional)
                        </label>
                        <textarea
                            id="bookingDescription"
                            rows={3}
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            className="w-full rounded-lg border border-slate-300 dark:border-slate-700 px-3 py-2 text-sm text-slate-900 dark:text-white bg-white dark:bg-slate-800 placeholder:text-slate-400 dark:placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500"
                            placeholder="Anything your trainer should know?"
                        />
                    </div>

                    <div className="flex justify-end gap-2">
                        <Button type="button" variant="ghost" onClick={() => setStep(2)} disabled={submitting}>Back</Button>
                        <Button type="button" disabled={submitting} onClick={handleConfirm}>
                            {submitting ? "Requesting..." : "Send request"}
                        </Button>
                    </div>
                </div>
            )}
        </div>
    );
}
