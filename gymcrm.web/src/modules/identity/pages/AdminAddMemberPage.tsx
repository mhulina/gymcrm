import React, {useEffect, useState} from "react";
import {Link} from "react-router-dom";
import AppLayout from "../../../app/AppLayout";
import {TextField} from "../../../shared/components/TextField";
import {SelectField} from "../../../shared/components/SelectField";
import {Button} from "../../../shared/components/Button";
import {Banner} from "../../../shared/components/Banner";
import {AccountTypeDropdown} from "../components/AccountTypeDropdown";
import {GymSubscriptionTypeDropdown} from "../components/GymSubscriptionTypeDropdown";
import {GenderDropdown} from "../components/GenderDropdown";
import {AccountType} from "../types/accountType";
import {GymSubscriptionType} from "../types/gymSubscriptionType";
import {Gender} from "../types/gender";
import {Member} from "../types/member";
import {adminCreateMember, fetchAllMembers} from "../api/identityApi";

interface FormState {
    firstName: string;
    middleName: string;
    lastName: string;
    email: string;
    password: string;
    phoneNumber: string;
    mobileNumber: string;
    gender: number;
    accountType: number;
    gymSubscriptionType: number;
    workingExperienceInMonths: string;
    personalTrainerId: string;
}

const initialForm: FormState = {
    firstName: "",
    middleName: "",
    lastName: "",
    email: "",
    password: "",
    phoneNumber: "",
    mobileNumber: "",
    gender: Gender.Male,
    accountType: AccountType.Member,
    gymSubscriptionType: GymSubscriptionType.Monthly,
    workingExperienceInMonths: "",
    personalTrainerId: "",
};

export default function AdminAddMemberPage() {
    const [form, setForm] = useState<FormState>(initialForm);
    const [trainers, setTrainers] = useState<Member[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);
    const [submitting, setSubmitting] = useState(false);

    useEffect(() => {
        fetchAllMembers().then((members) => {
            setTrainers(members.filter((m) => m.accountType === AccountType.PersonalTrainer));
        });
    }, []);

    function update<K extends keyof FormState>(key: K, value: FormState[K]) {
        setForm((prev) => ({ ...prev, [key]: value }));
    }

    function validateForm() {
        return form.email.length > 0 && form.password.length > 0;
    }

    async function handleSubmit(event: { preventDefault: () => void }) {
        event.preventDefault();
        setError(null);
        setSuccess(false);
        setSubmitting(true);

        const created = await adminCreateMember({
            insertAccount: {
                email: form.email.trim(),
                password: form.password,
                accountType: form.accountType,
                gymSubscriptionType: form.gymSubscriptionType,
                gender: form.gender,
            },
            profile: {
                firstName: form.firstName || undefined,
                middleName: form.middleName || undefined,
                lastName: form.lastName || undefined,
                phoneNumber: form.phoneNumber || undefined,
                mobileNumber: form.mobileNumber || undefined,
                workingExperienceInMonths:
                    form.accountType === AccountType.PersonalTrainer && form.workingExperienceInMonths
                        ? Number(form.workingExperienceInMonths)
                        : undefined,
                personalTrainerId:
                    form.accountType === AccountType.Member && form.personalTrainerId
                        ? form.personalTrainerId
                        : undefined,
            },
        });

        if (created) {
            setSuccess(true);
            setForm(initialForm);
        } else {
            setError("We couldn't create that member. The email may already be registered.");
        }

        setSubmitting(false);
    }

    return (
        <AppLayout showLogout>
            <Link to="/member/home" className="text-sm font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300">
                &larr; Back to dashboard
            </Link>

            <div className="mt-4 rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 sm:p-8 shadow-sm">
                <h1 className="text-xl font-bold text-slate-900 dark:text-white">Add a new member</h1>
                <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
                    Create an account and set up their profile in one step.
                </p>

                {success && <div className="mt-6"><Banner variant="success">Member created successfully.</Banner></div>}
                {error && <div className="mt-6"><Banner variant="error">{error}</Banner></div>}

                <form onSubmit={handleSubmit} className="mt-6 space-y-5">
                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                        <TextField id="firstName" label="First name" value={form.firstName} onChange={(e) => update("firstName", e.target.value)} />
                        <TextField id="middleName" label="Middle name" value={form.middleName} onChange={(e) => update("middleName", e.target.value)} />
                        <TextField id="lastName" label="Last name" value={form.lastName} onChange={(e) => update("lastName", e.target.value)} />
                    </div>

                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <TextField
                            id="email"
                            label="Email"
                            type="email"
                            required
                            placeholder="name@example.com"
                            value={form.email}
                            onChange={(e) => update("email", e.target.value)}
                        />
                        <TextField
                            id="password"
                            label="Temporary password"
                            type="password"
                            required
                            value={form.password}
                            onChange={(e) => update("password", e.target.value)}
                        />
                    </div>

                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <TextField id="phoneNumber" label="Phone number" value={form.phoneNumber} onChange={(e) => update("phoneNumber", e.target.value)} />
                        <TextField id="mobileNumber" label="Mobile number" value={form.mobileNumber} onChange={(e) => update("mobileNumber", e.target.value)} />
                    </div>

                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                        <GenderDropdown id="gender" label="Gender" value={form.gender} onChange={(value) => update("gender", value)} />
                        <AccountTypeDropdown id="accountType" label="Account type" value={form.accountType} onChange={(value) => update("accountType", value)} />
                        <GymSubscriptionTypeDropdown id="gymSubscriptionType" label="Subscription" value={form.gymSubscriptionType} onChange={(value) => update("gymSubscriptionType", value)} />
                    </div>

                    {form.accountType === AccountType.PersonalTrainer && (
                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                            <TextField
                                id="workingExperienceInMonths"
                                label="Work experience (months)"
                                type="number"
                                min={0}
                                value={form.workingExperienceInMonths}
                                onChange={(e) => update("workingExperienceInMonths", e.target.value)}
                            />
                        </div>
                    )}

                    {form.accountType === AccountType.Member && (
                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
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
                        </div>
                    )}

                    <div className="pt-2">
                        <Button type="submit" disabled={!validateForm() || submitting}>
                            {submitting ? "Creating member..." : "Create member"}
                        </Button>
                    </div>
                </form>
            </div>
        </AppLayout>
    );
}
