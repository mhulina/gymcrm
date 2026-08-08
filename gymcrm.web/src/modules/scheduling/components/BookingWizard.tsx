import {useEffect, useMemo, useState} from "react";
import {Member} from "../../identity/types/member";
import {fullName, initials} from "../../identity/utils/memberDisplay";
import {AvailableSlot} from "../types/availableSlot";
import {InsertTrainingSession} from "../types/insertTrainingSession";
import {addTrainingSession, fetchAvailableSlotsForTrainer} from "../api/schedulingApi";
import {
    addMinutesToTime,
    buildLocalDateTime,
    convertWallClock,
    isPastDate,
    parseLocalDateTime,
    toDateInputValue,
    toTimeInputValue,
} from "../utils/calendarDate";
import {Button} from "../../../shared/components/Button";
import {Banner} from "../../../shared/components/Banner";
import {TextField} from "../../../shared/components/TextField";
import {SelectField} from "../../../shared/components/SelectField";

const STEP_LABELS = ["Select Trainer", "Choose Date & Time", "Send Request"] as const;

interface Props {
    member: Member;
    trainers: Member[];
    trainersLoading: boolean;
    initialDate: Date;
    onClose: () => void;
    onBooked: () => void;
}

interface MemberLocalSlot {
    time: string;
    durations: number[];
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
    const [date, setDate] = useState(toDateInputValue(initialDate));
    const [availableSlots, setAvailableSlots] = useState<AvailableSlot[]>([]);
    const [slotsLoading, setSlotsLoading] = useState(false);
    const [startTime, setStartTime] = useState("");
    const [duration, setDuration] = useState<30 | 60 | 90 | null>(null);
    const [description, setDescription] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const memberZone = member.timeZone;
    const selectedTrainer = trainers.find((t) => t.accountGuid === selectedTrainerId);
    const trainerZone = selectedTrainer?.timeZone;
    const selectedDate = useMemo(() => parseDateInputValue(date), [date]);

    // Available start times/durations depend on the trainer's own schedule for that specific
    // calendar day (working hours, existing sessions, time off, holidays) - re-fetched whenever
    // the trainer or date changes. Noon is used as the reference instant for converting the
    // member's selected date into the trainer's local calendar date (a stable middle-of-day
    // choice, avoiding the near-midnight edge case this file already treats elsewhere as an
    // accepted soft trade-off).
    useEffect(() => {
        if (!selectedTrainerId || !trainerZone) {
            setAvailableSlots([]);
            return;
        }
        setSlotsLoading(true);
        const trainerLocalDate = convertWallClock(date, "12:00", memberZone, trainerZone).date;
        fetchAvailableSlotsForTrainer(selectedTrainerId, trainerLocalDate)
            .then(setAvailableSlots)
            .finally(() => setSlotsLoading(false));
    }, [selectedTrainerId, date, trainerZone, memberZone]);

    // Converts each raw trainer-local slot into the member's own zone (same per-slot conversion
    // pattern MemberSessionCalendar.tsx already uses for sessions), then keeps only the ones that
    // still land on the member's currently-selected calendar date - guards the rare cross-midnight
    // case, same accepted trade-off as the rest of this file.
    const slotsInMemberZone = useMemo<MemberLocalSlot[]>(() => {
        if (!trainerZone) return [];
        return availableSlots
            .map((slot) => {
                const raw = parseLocalDateTime(slot.startTime);
                const local = convertWallClock(toDateInputValue(raw), toTimeInputValue(raw), trainerZone, memberZone);
                return {date: local.date, time: local.time, durations: slot.availableDurationsMinutes};
            })
            .filter((s) => s.date === date)
            .sort((a, b) => a.time.localeCompare(b.time))
            .map(({time, durations}) => ({time, durations}));
    }, [availableSlots, trainerZone, memberZone, date]);

    // Keeps startTime/duration pointing at a currently-offered slot whenever the slot list
    // changes underneath them (new trainer, new date, or a slot that got taken) - falls back to
    // the first available slot, or clears out entirely once nothing is available.
    useEffect(() => {
        if (slotsInMemberZone.length === 0) {
            setStartTime("");
            setDuration(null);
            return;
        }

        const stillValid = slotsInMemberZone.find((s) => s.time === startTime);
        if (stillValid) {
            if (!duration || !stillValid.durations.includes(duration)) {
                setDuration(stillValid.durations[0] as 30 | 60 | 90);
            }
            return;
        }

        setStartTime(slotsInMemberZone[0].time);
        setDuration(slotsInMemberZone[0].durations[0] as 30 | 60 | 90);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [slotsInMemberZone]);

    const selectedSlot = slotsInMemberZone.find((s) => s.time === startTime);
    const endTime = startTime && duration ? addMinutesToTime(startTime, duration) : "";

    function handleClose() {
        onClose();
    }

    async function handleConfirm() {
        if (!member.accountGuid || !selectedTrainerId || !trainerZone || !startTime || !duration) return;
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
                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <TextField
                            id="bookingDate"
                            label="Date"
                            type="date"
                            min={toDateInputValue(new Date())}
                            value={date}
                            onChange={(e) => setDate(e.target.value)}
                        />
                        <SelectField
                            id="bookingStartTime"
                            label="Start time"
                            value={startTime}
                            disabled={slotsLoading || slotsInMemberZone.length === 0}
                            onChange={(e) => setStartTime(e.target.value)}
                        >
                            {slotsInMemberZone.length === 0 ? (
                                <option value="">No available times</option>
                            ) : (
                                slotsInMemberZone.map((slot) => (
                                    <option key={slot.time} value={slot.time}>{slot.time}</option>
                                ))
                            )}
                        </SelectField>
                    </div>

                    {slotsLoading ? (
                        <p className="text-sm text-slate-500 dark:text-slate-400">Checking availability...</p>
                    ) : slotsInMemberZone.length === 0 ? (
                        <Banner variant="info">No available times on this date.</Banner>
                    ) : (
                        <div>
                            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1.5">Duration</label>
                            <div className="flex gap-2">
                                {(selectedSlot?.durations ?? []).map((minutes) => (
                                    <button
                                        key={minutes}
                                        type="button"
                                        onClick={() => setDuration(minutes as 30 | 60 | 90)}
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
                    )}

                    <div className="flex justify-end gap-2">
                        <Button type="button" variant="ghost" onClick={() => setStep(1)}>Back</Button>
                        <Button type="button" disabled={isPastDate(selectedDate) || !startTime || !duration} onClick={() => setStep(3)}>
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
