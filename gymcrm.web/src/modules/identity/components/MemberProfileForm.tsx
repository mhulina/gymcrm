import {useEffect, useState} from "react";
import {TextField} from "../../../shared/components/TextField";
import {Button} from "../../../shared/components/Button";
import {Banner} from "../../../shared/components/Banner";
import {AvatarPicker} from "../../../shared/components/AvatarPicker";
import {UnsavedFieldNote} from "../../../shared/components/UnsavedFieldNote";
import {AccountTypeDropdown} from "./AccountTypeDropdown";
import {GymSubscriptionTypeDropdown} from "./GymSubscriptionTypeDropdown";
import {GenderDropdown} from "./GenderDropdown";
import {SelectField} from "../../../shared/components/SelectField";
import {AccountType} from "../types/accountType";
import {Member} from "../types/member";
import {fetchAllMembers, updateMember} from "../api/identityApi";
import {initials} from "../utils/memberDisplay";
import {TrainerWeeklyScheduleEditor} from "../../scheduling/components/TrainerWeeklyScheduleEditor";

interface FormState {
    firstName: string;
    middleName: string;
    lastName: string;
    gender: number;
    phoneNumber: string;
    mobileNumber: string;
    timeZone: string;
    accountType: number;
    gymSubscriptionType: number;
    personalTrainerId: string;
    workingExperienceInMonths: string;
}

// TimeZone is hardcoded to "UTC" for every account at registration time (pre-dating
// this fix) unless the member has already saved a real one here - fall back to the
// browser's own detected zone as a sensible default rather than making them hunt
// for it, since it's almost certainly correct.
function defaultTimeZone(member: Member): string {
    if (member.timeZone && member.timeZone !== "UTC") {
        return member.timeZone;
    }
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}

function formFromMember(member: Member): FormState {
    return {
        firstName: member.firstName ?? "",
        middleName: member.middleName ?? "",
        lastName: member.lastName ?? "",
        gender: member.gender,
        phoneNumber: member.phoneNumber ?? "",
        mobileNumber: member.mobileNumber ?? "",
        timeZone: defaultTimeZone(member),
        accountType: member.accountType,
        gymSubscriptionType: member.gymSubscriptionType,
        personalTrainerId: member.personalTrainerId ?? "",
        workingExperienceInMonths: member.workingExperienceInMonths?.toString() ?? "",
    };
}

const TIME_ZONES = Intl.supportedValuesOf("timeZone");

interface Props {
    targetMember: Member;
    viewerRole: number;
    isSelf: boolean;
    onSaved: () => void;
}

export function MemberProfileForm({ targetMember, viewerRole, isSelf, onSaved }: Props) {
    const [form, setForm] = useState<FormState>(() => formFromMember(targetMember));
    const [dateOfBirth, setDateOfBirth] = useState("");
    const [hourlyPrice, setHourlyPrice] = useState("");
    const [photoPreviewUrl, setPhotoPreviewUrl] = useState<string | null>(null);
    const [trainers, setTrainers] = useState<Member[]>([]);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);

    const isAdmin = viewerRole === AccountType.Admin;
    const showTrainerFields =
        (isAdmin && form.accountType === AccountType.PersonalTrainer) ||
        (viewerRole === AccountType.PersonalTrainer && isSelf);
    // Gated on the *persisted* account type, not the live form selection - the
    // weekly schedule lives in a different module with no transaction tying it
    // to this save, so showing it before a promotion is actually saved risks an
    // orphaned schedule for someone who's still a Member.
    const showScheduleEditor =
        targetMember.accountType === AccountType.PersonalTrainer &&
        (isAdmin || (viewerRole === AccountType.PersonalTrainer && isSelf));
    const pendingPromotionToTrainer =
        isAdmin && form.accountType === AccountType.PersonalTrainer && targetMember.accountType !== AccountType.PersonalTrainer;

    useEffect(() => {
        setForm(formFromMember(targetMember));
    }, [targetMember]);

    useEffect(() => {
        if (isAdmin) {
            fetchAllMembers().then((members) => {
                setTrainers(members.filter((m) => m.accountType === AccountType.PersonalTrainer));
            });
        }
    }, [isAdmin]);

    function update<K extends keyof FormState>(key: K, value: FormState[K]) {
        setForm((prev) => ({ ...prev, [key]: value }));
    }

    async function handleSubmit(event: { preventDefault: () => void }) {
        event.preventDefault();
        setError(null);
        setSuccess(false);
        setSubmitting(true);

        // Always start from the fully-fetched record and layer edits on top - the
        // backend expects the complete DTO on every save (see MemberProfileForm's
        // module-level notes / the plan this feature was built from), not a sparse
        // patch, so any field this form doesn't surface must still round-trip.
        const payload: Member = {
            ...targetMember,
            firstName: form.firstName || undefined,
            middleName: form.middleName || undefined,
            lastName: form.lastName || undefined,
            phoneNumber: form.phoneNumber || undefined,
            mobileNumber: form.mobileNumber || undefined,
            gender: form.gender,
            timeZone: form.timeZone,
            ...(isAdmin
                ? {
                      accountType: form.accountType,
                      gymSubscriptionType: form.gymSubscriptionType,
                      personalTrainerId:
                          form.accountType === AccountType.Member && form.personalTrainerId
                              ? form.personalTrainerId
                              : undefined,
                  }
                : {}),
            ...(showTrainerFields
                ? {
                      workingExperienceInMonths: form.workingExperienceInMonths
                          ? Number(form.workingExperienceInMonths)
                          : undefined,
                  }
                : {}),
        };

        const saved = await updateMember(payload);

        if (saved) {
            setSuccess(true);
            onSaved();
        } else {
            setError("We couldn't save these changes.");
        }

        setSubmitting(false);
    }

    return (
        <div className="space-y-6">
            <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 sm:p-8 shadow-sm">
                {success && <Banner variant="success">Profile saved.</Banner>}
                {error && <Banner variant="error">{error}</Banner>}

                <form onSubmit={handleSubmit} className="space-y-5">
                    <AvatarPicker
                        initials={initials(targetMember)}
                        previewUrl={photoPreviewUrl}
                        onFileSelected={(_file, previewUrl) => setPhotoPreviewUrl(previewUrl)}
                    />

                    <div>
                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1.5">
                            Email
                        </label>
                        <p className="rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/50 px-3 py-2 text-sm text-slate-500 dark:text-slate-400">
                            {targetMember.email}
                        </p>
                    </div>

                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                        <TextField id="firstName" label="First name" value={form.firstName} onChange={(e) => update("firstName", e.target.value)} />
                        <TextField id="middleName" label="Middle name" value={form.middleName} onChange={(e) => update("middleName", e.target.value)} />
                        <TextField id="lastName" label="Last name" value={form.lastName} onChange={(e) => update("lastName", e.target.value)} />
                    </div>

                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <TextField id="phoneNumber" label="Phone number" value={form.phoneNumber} onChange={(e) => update("phoneNumber", e.target.value)} />
                        <TextField id="mobileNumber" label="Mobile number" value={form.mobileNumber} onChange={(e) => update("mobileNumber", e.target.value)} />
                    </div>

                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <GenderDropdown id="gender" label="Gender" value={form.gender} onChange={(value) => update("gender", value)} />
                        <div>
                            <TextField
                                id="dateOfBirth"
                                label="Date of birth"
                                type="date"
                                value={dateOfBirth}
                                onChange={(e) => setDateOfBirth(e.target.value)}
                            />
                            <UnsavedFieldNote />
                        </div>
                    </div>

                    <SelectField
                        id="timeZone"
                        label="Time zone"
                        value={form.timeZone}
                        onChange={(e) => update("timeZone", e.target.value)}
                    >
                        {TIME_ZONES.map((zone) => (
                            <option key={zone} value={zone}>{zone}</option>
                        ))}
                    </SelectField>

                    {isAdmin && (
                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 border-t border-slate-100 dark:border-slate-800 pt-5">
                            <AccountTypeDropdown id="accountType" label="Account type" value={form.accountType} onChange={(value) => update("accountType", value)} />
                            <GymSubscriptionTypeDropdown id="gymSubscriptionType" label="Subscription" value={form.gymSubscriptionType} onChange={(value) => update("gymSubscriptionType", value)} />
                        </div>
                    )}

                    {isAdmin && form.accountType === AccountType.Member && (
                        <SelectField
                            id="personalTrainerId"
                            label="Assign personal trainer"
                            value={form.personalTrainerId}
                            onChange={(e) => update("personalTrainerId", e.target.value)}
                        >
                            <option value="">No trainer assigned</option>
                            {trainers.map((trainer) => (
                                <option key={trainer.accountGuid} value={trainer.accountGuid}>
                                    {trainer.firstName ?? trainer.email} {trainer.lastName ?? ""}
                                </option>
                            ))}
                        </SelectField>
                    )}

                    {showTrainerFields && (
                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 border-t border-slate-100 dark:border-slate-800 pt-5">
                            <TextField
                                id="workingExperienceInMonths"
                                label="Work experience (months)"
                                type="number"
                                min={0}
                                value={form.workingExperienceInMonths}
                                onChange={(e) => update("workingExperienceInMonths", e.target.value)}
                            />
                            <div>
                                <TextField
                                    id="hourlyPrice"
                                    label="Hourly price"
                                    type="number"
                                    min={0}
                                    step="0.01"
                                    value={hourlyPrice}
                                    onChange={(e) => setHourlyPrice(e.target.value)}
                                />
                                <UnsavedFieldNote />
                            </div>
                        </div>
                    )}

                    <div className="pt-2">
                        <Button type="submit" disabled={submitting}>
                            {submitting ? "Saving..." : "Save changes"}
                        </Button>
                    </div>
                </form>
            </div>

            {showScheduleEditor && (
                <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 sm:p-8 shadow-sm">
                    <h2 className="text-lg font-bold text-slate-900 dark:text-white">Weekly schedule</h2>
                    <p className="mt-1 mb-4 text-sm text-slate-500 dark:text-slate-400">
                        Which days this trainer works and their hours.
                    </p>
                    <TrainerWeeklyScheduleEditor trainerId={targetMember.accountGuid ?? ""} />
                </div>
            )}

            {pendingPromotionToTrainer && !showScheduleEditor && (
                <Banner variant="info">
                    Save this change, then reopen the page to set their weekly schedule.
                </Banner>
            )}
        </div>
    );
}
