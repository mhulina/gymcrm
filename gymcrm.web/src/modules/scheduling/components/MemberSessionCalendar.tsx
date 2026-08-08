import {useEffect, useMemo, useState} from "react";
import {Member} from "../../identity/types/member";
import {AccountType} from "../../identity/types/accountType";
import {fetchAllMembers} from "../../identity/api/identityApi";
import {TrainingSession} from "../types/trainingSession";
import {TrainingSessionStatus} from "../types/trainingSessionStatus";
import {TimeOff} from "../types/timeOff";
import {TrainerAvailability} from "../types/trainerAvailability";
import {
    fetchAvailabilitiesForTrainer,
    fetchTimeOffForTrainer,
    fetchTrainerIdsWithWorkingHours,
    fetchTrainingSessionsForClient,
} from "../api/schedulingApi";
import {
    buildLocalDateTime,
    convertWallClock,
    dayOfWeekName,
    isPastDate,
    isSameDay,
    parseLocalDateTime,
    toDateInputValue,
    toTimeInputValue,
} from "../utils/calendarDate";
import {Button} from "../../../shared/components/Button";
import {Badge} from "../../../shared/components/Badge";
import {enumLabel} from "../../../shared/utils/mapper";
import {BookingWizard} from "./BookingWizard";

const MONTH_FORMATTER = new Intl.DateTimeFormat(undefined, {month: "long", year: "numeric"});
const DAY_FORMATTER = new Intl.DateTimeFormat(undefined, {weekday: "long", month: "long", day: "numeric"});
const TIME_FORMATTER = new Intl.DateTimeFormat(undefined, {hour: "2-digit", minute: "2-digit"});
const WEEKDAY_LABELS = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];

function startOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
}

function buildMonthGrid(monthDate: Date): Date[] {
    const firstOfMonth = startOfMonth(monthDate);
    const gridStart = new Date(firstOfMonth);
    gridStart.setDate(gridStart.getDate() - firstOfMonth.getDay());
    return Array.from({length: 42}, (_, i) => {
        const day = new Date(gridStart);
        day.setDate(gridStart.getDate() + i);
        return day;
    });
}

export function MemberSessionCalendar({member}: { member: Member }) {
    const [visibleMonth, setVisibleMonth] = useState(() => startOfMonth(new Date()));
    const [sessions, setSessions] = useState<TrainingSession[]>([]);
    const [loadingSessions, setLoadingSessions] = useState(true);
    const [trainerAvailability, setTrainerAvailability] = useState<TrainerAvailability | null>(null);
    const [trainerTimeOff, setTrainerTimeOff] = useState<TimeOff[]>([]);
    const [trainers, setTrainers] = useState<Member[]>([]);
    const [trainersLoading, setTrainersLoading] = useState(true);
    const [selectedDay, setSelectedDay] = useState<Date | null>(null);
    const [wizardOpen, setWizardOpen] = useState(false);

    function reloadSessions() {
        if (!member.accountGuid) return;
        setLoadingSessions(true);
        fetchTrainingSessionsForClient(member.accountGuid)
            .then(setSessions)
            .finally(() => setLoadingSessions(false));
    }

    // eslint-disable-next-line react-hooks/exhaustive-deps
    useEffect(reloadSessions, [member.accountGuid]);

    useEffect(() => {
        Promise.all([fetchAllMembers(), fetchTrainerIdsWithWorkingHours()]).then(([members, trainerIdsWithHours]) => {
            const bookableTrainerIds = new Set(trainerIdsWithHours);
            const bookableTrainers = members.filter(
                (m) => m.accountType === AccountType.PersonalTrainer && m.accountGuid && bookableTrainerIds.has(m.accountGuid)
            );
            // Once a member has chosen their own trainer, that's the only one they can book
            // with - if that trainer isn't currently bookable (no working hours yet), the
            // resulting empty list correctly surfaces BookingWizard's existing "No trainers
            // are available to book with yet." state rather than a new one.
            setTrainers(
                member.personalTrainerId
                    ? bookableTrainers.filter((t) => t.accountGuid === member.personalTrainerId)
                    : bookableTrainers
            );
            setTrainersLoading(false);
        });
    }, [member.personalTrainerId]);

    useEffect(() => {
        if (!member.personalTrainerId) return;
        fetchAvailabilitiesForTrainer(member.personalTrainerId).then((list) => setTrainerAvailability(list[0] ?? null));
        fetchTimeOffForTrainer(member.personalTrainerId).then(setTrainerTimeOff);
    }, [member.personalTrainerId]);

    // Day-off/time-off shading below is intentionally NOT cross-timezone-corrected - it
    // keeps comparing the trainer's own naive weekday/date directly. It's a soft,
    // non-authoritative hint (the server has final say on any actual booking), and fully
    // correcting it would mean a day near a timezone's date-line offset could dim a
    // different calendar cell than the trainer's real day off - disproportionate for a
    // dashboard hint.
    const dayOffWeekdays = useMemo(() => {
        const set = new Set<string>();
        (trainerAvailability?.dailyAvailabilities ?? []).forEach((daily) => {
            if (daily.isDayOff) set.add(daily.dayOfWeek);
        });
        return set;
    }, [trainerAvailability]);

    const timeOffDates = useMemo(
        () => new Set(trainerTimeOff.map((entry) => toDateInputValue(parseLocalDateTime(entry.date)))),
        [trainerTimeOff]
    );

    const trainerZoneById = useMemo(() => {
        const map = new Map<string, string>();
        trainers.forEach((trainer) => {
            if (trainer.accountGuid) map.set(trainer.accountGuid, trainer.timeZone);
        });
        return map;
    }, [trainers]);

    // A member can have sessions with different trainers over time, each stored in that
    // trainer's own local wall-clock time (see insertTrainingSession.ts) - convert each
    // session into the member's own timezone individually before grouping/displaying, so
    // both the calendar's day marker and the day panel's times reflect the booking
    // member's timezone, per-session, not a single assumed zone.
    const sessionsByDay = useMemo(() => {
        const map = new Map<string, {session: TrainingSession; start: Date; end: Date}[]>();
        sessions
            .filter((session) => session.status !== TrainingSessionStatus.Cancelled)
            .forEach((session) => {
                const trainerZone = trainerZoneById.get(session.trainerId);
                const rawStart = parseLocalDateTime(session.startTime);
                const rawEnd = parseLocalDateTime(session.endTime);
                const startLocal = trainerZone
                    ? convertWallClock(toDateInputValue(rawStart), toTimeInputValue(rawStart), trainerZone, member.timeZone)
                    : {date: toDateInputValue(rawStart), time: toTimeInputValue(rawStart)};
                const endLocal = trainerZone
                    ? convertWallClock(toDateInputValue(rawEnd), toTimeInputValue(rawEnd), trainerZone, member.timeZone)
                    : {date: toDateInputValue(rawEnd), time: toTimeInputValue(rawEnd)};

                const entry = {
                    session,
                    start: parseLocalDateTime(buildLocalDateTime(startLocal.date, startLocal.time)),
                    end: parseLocalDateTime(buildLocalDateTime(endLocal.date, endLocal.time)),
                };
                const list = map.get(startLocal.date) ?? [];
                list.push(entry);
                map.set(startLocal.date, list);
            });
        return map;
    }, [sessions, trainerZoneById, member.timeZone]);

    const days = useMemo(() => buildMonthGrid(visibleMonth), [visibleMonth]);
    const today = new Date();

    function selectDay(day: Date) {
        setWizardOpen(false);
        setSelectedDay(day);
    }

    const selectedDaySessions = selectedDay ? sessionsByDay.get(toDateInputValue(selectedDay)) ?? [] : [];

    if (!member.accountGuid) return null;

    return (
        <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-sm">
            <div className="flex items-center justify-between">
                <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
                    Training sessions
                </h2>
                <div className="flex items-center gap-3">
                    <button
                        type="button"
                        aria-label="Previous month"
                        onClick={() => setVisibleMonth((m) => new Date(m.getFullYear(), m.getMonth() - 1, 1))}
                        className="rounded-lg px-2 py-1 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
                    >
                        &lsaquo;
                    </button>
                    <span className="text-sm font-semibold text-slate-800 dark:text-slate-100">
                        {MONTH_FORMATTER.format(visibleMonth)}
                    </span>
                    <button
                        type="button"
                        aria-label="Next month"
                        onClick={() => setVisibleMonth((m) => new Date(m.getFullYear(), m.getMonth() + 1, 1))}
                        className="rounded-lg px-2 py-1 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
                    >
                        &rsaquo;
                    </button>
                </div>
            </div>

            <div className="mt-4 grid grid-cols-7 gap-1 text-center text-xs">
                {WEEKDAY_LABELS.map((label) => (
                    <div key={label} className="py-1 font-bold text-slate-400 dark:text-slate-500">
                        {label}
                    </div>
                ))}
                {days.map((day) => {
                    const inMonth = day.getMonth() === visibleMonth.getMonth();
                    const key = toDateInputValue(day);
                    const hasSessions = sessionsByDay.has(key);
                    const past = isPastDate(day);
                    const dimmed = dayOffWeekdays.has(dayOfWeekName(day)) || timeOffDates.has(key);
                    const isToday = isSameDay(day, today);
                    const isSelected = selectedDay ? isSameDay(day, selectedDay) : false;

                    return (
                        <button
                            key={key}
                            type="button"
                            disabled={past}
                            onClick={() => selectDay(day)}
                            className={[
                                "relative flex h-9 flex-col items-center justify-center rounded-lg text-xs font-semibold transition-colors",
                                !inMonth ? "text-slate-300 dark:text-slate-700" : dimmed ? "text-slate-300 dark:text-slate-600" : "text-slate-700 dark:text-slate-200",
                                past ? "cursor-not-allowed opacity-40" : "cursor-pointer hover:bg-emerald-50 dark:hover:bg-emerald-950/40",
                                isSelected ? "bg-emerald-600 text-white hover:bg-emerald-600" : "",
                                isToday && !isSelected ? "ring-1 ring-emerald-500" : "",
                            ].join(" ")}
                        >
                            {day.getDate()}
                            {hasSessions && (
                                <span className={`absolute bottom-1 h-1 w-1 rounded-full ${isSelected ? "bg-white" : "bg-emerald-500"}`} />
                            )}
                        </button>
                    );
                })}
            </div>

            {member.personalTrainerId && (
                <p className="mt-3 text-xs text-slate-400 dark:text-slate-500">
                    Dimmed days are your trainer&apos;s day off or time off.
                </p>
            )}

            {selectedDay && (
                <div className="mt-5 border-t border-slate-100 dark:border-slate-800 pt-4">
                    <h3 className="text-sm font-bold text-slate-900 dark:text-white">
                        {DAY_FORMATTER.format(selectedDay)}
                    </h3>

                    {loadingSessions ? (
                        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Loading...</p>
                    ) : selectedDaySessions.length > 0 ? (
                        <ul className="mt-2 space-y-2">
                            {selectedDaySessions.map(({session, start, end}) => (
                                <li
                                    key={session.id}
                                    className="flex items-center justify-between rounded-lg border border-slate-200 dark:border-slate-800 px-3 py-2 text-sm"
                                >
                                    <span className="text-slate-700 dark:text-slate-200">
                                        {TIME_FORMATTER.format(start)}
                                        {" – "}
                                        {TIME_FORMATTER.format(end)}
                                    </span>
                                    <Badge tone={session.status === TrainingSessionStatus.Booked ? "emerald" : "slate"}>
                                        {enumLabel(TrainingSessionStatus[session.status])}
                                    </Badge>
                                </li>
                            ))}
                        </ul>
                    ) : (
                        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">No sessions booked yet.</p>
                    )}

                    {!wizardOpen && !isPastDate(selectedDay) && (
                        <Button type="button" className="mt-3" onClick={() => setWizardOpen(true)}>
                            {selectedDaySessions.length > 0 ? "Book another session" : "Book a session"}
                        </Button>
                    )}

                    {wizardOpen && (
                        <BookingWizard
                            member={member}
                            trainers={trainers}
                            trainersLoading={trainersLoading}
                            initialDate={selectedDay}
                            onClose={() => setWizardOpen(false)}
                            onBooked={() => {
                                reloadSessions();
                                setWizardOpen(false);
                            }}
                        />
                    )}
                </div>
            )}
        </div>
    );
}
