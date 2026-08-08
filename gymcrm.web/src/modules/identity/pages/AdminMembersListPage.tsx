import {useEffect, useMemo, useState} from "react";
import {Link} from "react-router-dom";
import AppLayout from "../../../app/AppLayout";
import {Badge} from "../../../shared/components/Badge";
import {TextField} from "../../../shared/components/TextField";
import {AccountType} from "../types/accountType";
import {GymSubscriptionType} from "../types/gymSubscriptionType";
import {Member} from "../types/member";
import {fetchAllMembers} from "../api/identityApi";
import {fullName} from "../utils/memberDisplay";
import {enumLabel} from "../../../shared/utils/mapper";

export default function AdminMembersListPage() {
    const [members, setMembers] = useState<Member[]>([]);
    const [loading, setLoading] = useState(true);
    const [filter, setFilter] = useState("");

    useEffect(() => {
        fetchAllMembers()
            .then(setMembers)
            .finally(() => setLoading(false));
    }, []);

    const filteredMembers = useMemo(() => {
        const query = filter.trim().toLowerCase();
        if (!query) {
            return members;
        }
        return members.filter(
            (m) => fullName(m).toLowerCase().includes(query) || m.email.toLowerCase().includes(query)
        );
    }, [members, filter]);

    return (
        <AppLayout showLogout>
            <Link to="/member/home" className="text-sm font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300">
                &larr; Back to dashboard
            </Link>

            <div className="mt-4 flex flex-wrap items-center justify-between gap-4">
                <h1 className="text-xl font-bold text-slate-900 dark:text-white">Members</h1>
                <Link
                    to="/admin/members/new"
                    className="inline-flex items-center justify-center rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700 transition-colors"
                >
                    + Add member
                </Link>
            </div>

            <div className="mt-4 max-w-sm">
                <TextField
                    id="filter"
                    label="Search"
                    placeholder="Search by name or email"
                    value={filter}
                    onChange={(e) => setFilter(e.target.value)}
                />
            </div>

            <div className="mt-6 overflow-x-auto rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-sm">
                <table className="w-full text-left text-sm">
                    <thead className="border-b border-slate-200 dark:border-slate-800 text-xs uppercase tracking-wide text-slate-400 dark:text-slate-500">
                        <tr>
                            <th className="px-4 py-3 font-semibold">Name</th>
                            <th className="px-4 py-3 font-semibold">Email</th>
                            <th className="px-4 py-3 font-semibold">Role</th>
                            <th className="px-4 py-3 font-semibold">Subscription</th>
                            <th className="px-4 py-3" />
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                        {loading && (
                            <tr>
                                <td colSpan={5} className="px-4 py-6 text-center text-slate-400 dark:text-slate-500">
                                    Loading...
                                </td>
                            </tr>
                        )}
                        {!loading && filteredMembers.length === 0 && (
                            <tr>
                                <td colSpan={5} className="px-4 py-6 text-center text-slate-400 dark:text-slate-500">
                                    No members found.
                                </td>
                            </tr>
                        )}
                        {filteredMembers.map((member) => (
                            <tr key={member.accountGuid}>
                                <td className="px-4 py-3 font-medium text-slate-900 dark:text-white">{fullName(member)}</td>
                                <td className="px-4 py-3 text-slate-500 dark:text-slate-400">{member.email}</td>
                                <td className="px-4 py-3">
                                    <Badge>{enumLabel(AccountType[member.accountType])}</Badge>
                                </td>
                                <td className="px-4 py-3">
                                    <Badge tone="slate">{enumLabel(GymSubscriptionType[member.gymSubscriptionType])}</Badge>
                                </td>
                                <td className="px-4 py-3 text-right">
                                    <Link
                                        to={`/admin/members/${member.accountGuid}/edit`}
                                        className="text-sm font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300"
                                    >
                                        Edit
                                    </Link>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </AppLayout>
    );
}
