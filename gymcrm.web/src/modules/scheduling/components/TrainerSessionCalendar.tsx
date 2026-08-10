import {useEffect, useMemo, useState} from "react";
import {Member} from "../../identity/types/member";
import {fetchAllMembers} from "../../identity/api/identityApi";
import {fullName} from "../../identity/utils/memberDisplay";
import {TrainingSession} from "../types/trainingSession";
import {TrainingSessionStatus} from "../types/trainingSessionStatus";
import {isSameDay, parseLocalDateTime, toDateInputValue} from "../utils/calendarDate";
import {Badge} from "../../../shared/components/Badge";
import {enumLabel} from "../../../shared/utils/mapper";

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

interface Props {
    sessions: TrainingSession[];
    loading: boolean;
}

// Read-only glance view of a trainer's own sessions - no wizard, no accept/decline/reschedule
// actions (those live in TrainerSessionRequests). A trainer viewing their own sessions needs
// no timezone conversion at all, unlike MemberSessionCalendar. sessions/loading come from the
// shared parent (MemberInfoDashboard) rather than being fetched here, so accepting/declining/
// rescheduling a request in TrainerSessionRequests refreshes this calendar too.
export function TrainerSessionCalendar({sessions, loading}: Props) {
    const [visibleMonth, setVisibleMonth] = useState(() => startOfMonth(new Date()));
    const [clients, setClients] = useState<Member[]>([]);
    const [selectedDay, setSelectedDay] = useState<Date | null>(null);

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

    const sessionsByDay = useMemo(() => {
        const map = new Map<string, {session: TrainingSession; start: Date; end: Date}[]>();
        sessions
            .filter((session) => session.status !== TrainingSessionStatus.Cancelled)
            .forEach((session) => {
                const start = parseLocalDateTime(session.startTime);
                const end = parseLocalDateTime(session.endTime);
                const key = toDateInputValue(start);
                const list = map.get(key) ?? [];
                list.push({session, start, end});
                map.set(key, list);
            });
        return map;
    }, [sessions]);

    const days = useMemo(() => buildMonthGrid(visibleMonth), [visibleMonth]);
    const today = new Date();

    const selectedDaySessions = selectedDay ? sessionsByDay.get(toDateInputValue(selectedDay)) ?? [] : [];

    return (
        <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-sm">
            <div className="flex items-center justify-between">
                <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
                    My sessions
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
                    const isToday = isSameDay(day, today);
                    const isSelected = selectedDay ? isSameDay(day, selectedDay) : false;

                    return (
                        <button
                            key={key}
                            type="button"
                            onClick={() => setSelectedDay(day)}
                            className={[
                                "relative flex h-9 flex-col items-center justify-center rounded-lg text-xs font-semibold transition-colors cursor-pointer",
                                !inMonth ? "text-slate-300 dark:text-slate-700" : "text-slate-700 dark:text-slate-200",
                                "hover:bg-emerald-50 dark:hover:bg-emerald-950/40",
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

            {selectedDay && (
                <div className="mt-5 border-t border-slate-100 dark:border-slate-800 pt-4">
                    <h3 className="text-sm font-bold text-slate-900 dark:text-white">
                        {DAY_FORMATTER.format(selectedDay)}
                    </h3>

                    {loading ? (
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
                                        {" · "}
                                        {clientNameById.get(session.clientId) ?? "Unknown client"}
                                    </span>
                                    <Badge tone={session.status === TrainingSessionStatus.Booked ? "emerald" : "slate"}>
                                        {enumLabel(TrainingSessionStatus[session.status])}
                                    </Badge>
                                </li>
                            ))}
                        </ul>
                    ) : (
                        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">No sessions that day.</p>
                    )}
                </div>
            )}
        </div>
    );
}
