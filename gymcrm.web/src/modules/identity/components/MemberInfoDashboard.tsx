import {useEffect, useState} from "react";
import {Link} from "react-router-dom";
import {Member} from "../types/member";
import {AccountType} from "../types/accountType";
import {GymSubscriptionType} from "../types/gymSubscriptionType";
import {Gender} from "../types/gender";
import {fetchMemberByGuid} from "../api/identityApi";

function initials(member: Member) {
    const first = member.firstName?.[0] ?? member.email[0];
    const last = member.lastName?.[0] ?? "";
    return (first + last).toUpperCase();
}

function fullName(member: Member) {
    const parts = [member.firstName, member.lastName].filter(Boolean);
    return parts.length > 0 ? parts.join(" ") : member.email;
}

function Badge({ children, tone = "emerald" }: { children: React.ReactNode; tone?: "emerald" | "slate" }) {
    const tones = {
        emerald: "bg-emerald-50 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300",
        slate: "bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300",
    };
    return (
        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${tones[tone]}`}>
            {children}
        </span>
    );
}

function Card({ children, className = "" }: { children: React.ReactNode; className?: string }) {
    return (
        <div className={`rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-sm ${className}`}>
            {children}
        </div>
    );
}

export function MemberInfoDashboard({ userData }: { userData: Member }) {
    const [trainerName, setTrainerName] = useState<string | null>(null);

    useEffect(() => {
        if (userData.accountType === AccountType.Member && userData.personalTrainerId) {
            fetchMemberByGuid(userData.personalTrainerId).then((trainer) => {
                if (trainer) {
                    setTrainerName(fullName(trainer));
                }
            });
        }
    }, [userData.accountType, userData.personalTrainerId]);

    return (
        <div className="space-y-6">
            <Card className="flex flex-wrap items-center gap-4">
                <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-full bg-emerald-600 text-lg font-bold text-white">
                    {initials(userData)}
                </div>
                <div className="min-w-0 flex-1">
                    <h1 className="text-lg font-bold text-slate-900 dark:text-white truncate">{fullName(userData)}</h1>
                    <p className="text-sm text-slate-500 dark:text-slate-400 truncate">{userData.email}</p>
                    <div className="mt-2 flex gap-2">
                        <Badge>{AccountType[userData.accountType]}</Badge>
                        <Badge tone="slate">{GymSubscriptionType[userData.gymSubscriptionType]} plan</Badge>
                    </div>
                </div>
                {userData.accountType === AccountType.Admin && (
                    <Link
                        to="/admin/members/new"
                        className="inline-flex items-center justify-center rounded-lg border border-emerald-600 px-4 py-2.5 text-sm font-semibold text-emerald-600 hover:bg-emerald-50 dark:border-emerald-500 dark:text-emerald-400 dark:hover:bg-emerald-950/40 transition-colors"
                    >
                        + Add member
                    </Link>
                )}
            </Card>

            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <Card>
                    <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">Contact</h2>
                    <dl className="mt-3 space-y-2 text-sm">
                        <div className="flex justify-between gap-4">
                            <dt className="text-slate-500 dark:text-slate-400">Phone</dt>
                            <dd className="text-slate-900 dark:text-slate-100">{userData.phoneNumber || "Not provided"}</dd>
                        </div>
                        <div className="flex justify-between gap-4">
                            <dt className="text-slate-500 dark:text-slate-400">Mobile</dt>
                            <dd className="text-slate-900 dark:text-slate-100">{userData.mobileNumber || "Not provided"}</dd>
                        </div>
                        <div className="flex justify-between gap-4">
                            <dt className="text-slate-500 dark:text-slate-400">Gender</dt>
                            <dd className="text-slate-900 dark:text-slate-100">{Gender[userData.gender]}</dd>
                        </div>
                    </dl>
                </Card>

                <Card>
                    <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
                        {userData.accountType === AccountType.PersonalTrainer ? "Training profile" : "Membership"}
                    </h2>
                    <p className="mt-3 text-sm text-slate-900 dark:text-slate-100">
                        {userData.accountType === AccountType.PersonalTrainer
                            ? userData.workingExperienceInMonths
                                ? `${userData.workingExperienceInMonths} months of experience`
                                : "No experience on file yet"
                            : userData.accountType === AccountType.Member
                                ? trainerName
                                    ? `Trainer: ${trainerName}`
                                    : userData.personalTrainerId
                                        ? "Loading trainer..."
                                        : "No trainer assigned yet"
                                : "Administrator account"}
                    </p>
                </Card>
            </div>
        </div>
    );
}
