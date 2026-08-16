import {useEffect, useState} from "react";
import {Link} from "react-router-dom";
import {Member} from "../types/member";
import {AccountType} from "../types/accountType";
import {Gender} from "../types/gender";
import {fetchMemberByGuid} from "../api/identityApi";
import {fullName, initials} from "../utils/memberDisplay";
import {Badge} from "../../../shared/components/Badge";
import {enumLabel} from "../../../shared/utils/mapper";
import {TrainingSession} from "../../scheduling/types/trainingSession";
import {fetchTrainingSessionsForTrainer} from "../../scheduling/api/schedulingApi";
import {MemberSessionCalendar} from "../../scheduling/components/MemberSessionCalendar";
import {TrainerSessionRequests} from "../../scheduling/components/TrainerSessionRequests";
import {TrainerSessionCalendar} from "../../scheduling/components/TrainerSessionCalendar";
import {TrainerWorkingHoursSummary} from "../../scheduling/components/TrainerWorkingHoursSummary";
import {AvatarUploadButton} from "./AvatarUploadButton";

function Card({ children, className = "" }: { children: React.ReactNode; className?: string }) {
    return (
        <div className={`rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-sm ${className}`}>
            {children}
        </div>
    );
}

export function MemberInfoDashboard({ userData, onUserDataChanged }: { userData: Member; onUserDataChanged: () => void }) {
    const [trainerName, setTrainerName] = useState<string | null>(null);
    const [trainerSessions, setTrainerSessions] = useState<TrainingSession[]>([]);
    const [trainerSessionsLoading, setTrainerSessionsLoading] = useState(true);

    useEffect(() => {
        if (userData.accountType === AccountType.Member && userData.personalTrainerId) {
            fetchMemberByGuid(userData.personalTrainerId).then((trainer) => {
                if (trainer) {
                    setTrainerName(fullName(trainer));
                }
            });
        }
    }, [userData.accountType, userData.personalTrainerId]);

    // Shared between TrainerSessionRequests and TrainerSessionCalendar so accepting/declining/
    // rescheduling a request in one refreshes the other too, instead of each fetching its own
    // copy independently and only one of them updating.
    function reloadTrainerSessions() {
        if (!userData.accountGuid) return;
        setTrainerSessionsLoading(true);
        fetchTrainingSessionsForTrainer(userData.accountGuid)
            .then(setTrainerSessions)
            .finally(() => setTrainerSessionsLoading(false));
    }

    useEffect(() => {
        if (userData.accountType === AccountType.PersonalTrainer && userData.accountGuid) {
            reloadTrainerSessions();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [userData.accountType, userData.accountGuid]);

    return (
        <div className="space-y-6">
            <Card className="flex flex-wrap items-center gap-4">
                <AvatarUploadButton
                    accountGuid={userData.accountGuid ?? ""}
                    initials={initials(userData)}
                    hasPhoto={userData.hasPhoto}
                    onPhotoChanged={onUserDataChanged}
                />
                <div className="min-w-0 flex-1">
                    <h1 className="text-lg font-bold text-slate-900 dark:text-white truncate">{fullName(userData)}</h1>
                    <p className="text-sm text-slate-500 dark:text-slate-400 truncate">{userData.email}</p>
                    <div className="mt-2 flex gap-2">
                        <Badge>{enumLabel(AccountType[userData.accountType])}</Badge>
                    </div>
                </div>
                <div className="flex gap-2">
                    <Link
                        to="/member/edit"
                        className="inline-flex items-center justify-center rounded-lg border border-slate-300 dark:border-slate-700 px-4 py-2.5 text-sm font-semibold text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors"
                    >
                        Edit profile
                    </Link>
                    {userData.accountType === AccountType.Admin && (
                        <Link
                            to="/admin/members"
                            className="inline-flex items-center justify-center rounded-lg border border-emerald-600 px-4 py-2.5 text-sm font-semibold text-emerald-600 hover:bg-emerald-50 dark:border-emerald-500 dark:text-emerald-400 dark:hover:bg-emerald-950/40 transition-colors"
                        >
                            Manage members
                        </Link>
                    )}
                </div>
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
                            <dd className="text-slate-900 dark:text-slate-100">{enumLabel(Gender[userData.gender])}</dd>
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
                    {userData.accountType === AccountType.Member && (
                        <Link
                            to="/member/billing"
                            className="mt-2 inline-block text-sm font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300"
                        >
                            View subscription &rarr;
                        </Link>
                    )}
                </Card>
            </div>

            {(userData.accountType === AccountType.Member || userData.accountType === AccountType.Admin) && (
                <MemberSessionCalendar member={userData} />
            )}

            {userData.accountType === AccountType.PersonalTrainer && userData.accountGuid && (
                <>
                    <TrainerSessionRequests
                        sessions={trainerSessions}
                        loading={trainerSessionsLoading}
                        onChanged={reloadTrainerSessions}
                    />
                    <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                        <TrainerSessionCalendar sessions={trainerSessions} loading={trainerSessionsLoading} />
                        <TrainerWorkingHoursSummary trainerId={userData.accountGuid} />
                    </div>
                </>
            )}
        </div>
    );
}
