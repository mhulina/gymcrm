import {useEffect, useState} from "react";
import {Link, useNavigate, useParams} from "react-router-dom";
import AppLayout from "../../../app/AppLayout";
import {MemberProfileForm} from "../components/MemberProfileForm";
import {Member} from "../types/member";
import {fetchMemberByGuid, fetchUserInfoByGuid} from "../api/identityApi";

export default function EditMemberProfilePage() {
    const { guid } = useParams<{ guid?: string }>();
    const navigate = useNavigate();
    const [viewer, setViewer] = useState<Member | null>(null);
    const [target, setTarget] = useState<Member | null>(null);
    const [loading, setLoading] = useState(true);
    const [notFound, setNotFound] = useState(false);

    const isSelf = !guid;
    const backLink = isSelf ? "/member/home" : "/admin/members";

    function load() {
        setLoading(true);
        setNotFound(false);

        fetchUserInfoByGuid().then((viewerMember) => {
            setViewer(viewerMember);

            if (!guid) {
                setTarget(viewerMember);
                setLoading(false);
                return;
            }

            fetchMemberByGuid(guid).then((targetMember) => {
                setTarget(targetMember);
                setNotFound(!targetMember);
                setLoading(false);
            });
        });
    }

    useEffect(load, [guid]);

    if (loading) {
        return (
            <AppLayout showLogout>
                <p className="text-sm text-slate-500 dark:text-slate-400">Loading...</p>
            </AppLayout>
        );
    }

    if (notFound || !target || !viewer) {
        return (
            <AppLayout showLogout>
                <Link to={backLink} className="text-sm font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300">
                    &larr; Back
                </Link>
                <p className="mt-4 text-sm text-red-600 dark:text-red-400">We couldn't find that member.</p>
            </AppLayout>
        );
    }

    return (
        <AppLayout showLogout>
            <Link to={backLink} className="text-sm font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300">
                &larr; Back
            </Link>
            <h1 className="mt-4 mb-6 text-xl font-bold text-slate-900 dark:text-white">
                {isSelf ? "Edit your profile" : `Edit ${target.firstName ?? target.email}`}
            </h1>
            <MemberProfileForm
                targetMember={target}
                viewerRole={viewer.accountType}
                isSelf={isSelf}
                onSaved={() => navigate(backLink)}
            />
        </AppLayout>
    );
}
