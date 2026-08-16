import {useEffect, useState} from "react";
import AppLayout from "../../../app/AppLayout";
import {Badge} from "../../../shared/components/Badge";
import {Banner} from "../../../shared/components/Banner";
import {Button} from "../../../shared/components/Button";
import {enumLabel} from "../../../shared/utils/mapper";
import {fetchUserInfoByGuid} from "../../identity/api/identityApi";
import {Member} from "../../identity/types/member";
import {Subscription} from "../types/subscription";
import {Payment} from "../types/payment";
import {SubscriptionPlanType} from "../types/subscriptionPlanType";
import {SubscriptionStatus} from "../types/subscriptionStatus";
import {PaymentMethod} from "../types/paymentMethod";
import {PaymentStatus} from "../types/paymentStatus";
import {
    fetchActiveSubscriptionForMember,
    fetchSubscriptionsForMember,
    fetchPaymentsForMember,
    cancelSubscription
} from "../api/billingApi";

export default function MemberSubscriptionPage() {
    const [member, setMember] = useState<Member | null>(null);
    const [activeSubscription, setActiveSubscription] = useState<Subscription | null>(null);
    const [subscriptionHistory, setSubscriptionHistory] = useState<Subscription[]>([]);
    const [payments, setPayments] = useState<Payment[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    function load() {
        setLoading(true);

        fetchUserInfoByGuid().then((self) => {
            if (!self?.accountGuid) {
                setLoading(false);
                return;
            }

            setMember(self);

            Promise.all([
                fetchActiveSubscriptionForMember(self.accountGuid),
                fetchSubscriptionsForMember(self.accountGuid),
                fetchPaymentsForMember(self.accountGuid)
            ]).then(([activeResult, historyResult, paymentsResult]) => {
                setActiveSubscription(activeResult);
                setSubscriptionHistory(historyResult);
                setPayments(paymentsResult);
                setLoading(false);
            });
        });
    }

    // eslint-disable-next-line react-hooks/exhaustive-deps
    useEffect(load, []);

    async function handleCancel() {
        if (!activeSubscription) {
            return;
        }

        setError(null);
        setSubmitting(true);

        const result = await cancelSubscription(activeSubscription.id);

        if (result.success) {
            load();
        } else {
            setError(result.error ?? "We couldn't cancel this subscription.");
        }

        setSubmitting(false);
    }

    if (loading) {
        return (
            <AppLayout showLogout>
                <p className="text-sm text-slate-500 dark:text-slate-400">Loading...</p>
            </AppLayout>
        );
    }

    if (!member) {
        return (
            <AppLayout showLogout>
                <p className="text-sm text-red-600 dark:text-red-400">We couldn't load your subscription.</p>
            </AppLayout>
        );
    }

    return (
        <AppLayout showLogout>
            <h1 className="mb-6 text-xl font-bold text-slate-900 dark:text-white">My subscription</h1>

            {error && <Banner variant="error">{error}</Banner>}

            <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 sm:p-8 shadow-sm">
                {activeSubscription ? (
                    <div>
                        <div className="flex flex-wrap items-center gap-2">
                            <Badge>{enumLabel(SubscriptionPlanType[activeSubscription.planType])}</Badge>
                            <Badge tone="slate">{enumLabel(SubscriptionStatus[activeSubscription.status])}</Badge>
                        </div>
                        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
                            Started {new Date(activeSubscription.startDate).toLocaleDateString()}
                            {activeSubscription.nextRenewalDate && (
                                <> &middot; renews {new Date(activeSubscription.nextRenewalDate).toLocaleDateString()}</>
                            )}
                        </p>
                        <div className="mt-4">
                            <Button variant="secondary" disabled={submitting} onClick={handleCancel}>Cancel subscription</Button>
                        </div>
                    </div>
                ) : (
                    <p className="text-sm text-slate-500 dark:text-slate-400">
                        You don't have an active subscription. Ask front desk staff to set one up for you.
                    </p>
                )}
            </div>

            <div className="mt-6 rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 sm:p-8 shadow-sm">
                <h2 className="text-lg font-bold text-slate-900 dark:text-white">Subscription history</h2>
                <div className="mt-4 overflow-x-auto">
                    <table className="w-full text-left text-sm">
                        <thead className="border-b border-slate-200 dark:border-slate-800 text-xs uppercase tracking-wide text-slate-400 dark:text-slate-500">
                            <tr>
                                <th className="px-3 py-2 font-semibold">Plan</th>
                                <th className="px-3 py-2 font-semibold">Status</th>
                                <th className="px-3 py-2 font-semibold">Started</th>
                                <th className="px-3 py-2 font-semibold">Next renewal</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                            {subscriptionHistory.length === 0 && (
                                <tr>
                                    <td colSpan={4} className="px-3 py-4 text-center text-slate-400 dark:text-slate-500">
                                        No subscriptions yet.
                                    </td>
                                </tr>
                            )}
                            {subscriptionHistory.map((subscription) => (
                                <tr key={subscription.id}>
                                    <td className="px-3 py-2">{enumLabel(SubscriptionPlanType[subscription.planType])}</td>
                                    <td className="px-3 py-2">
                                        <Badge tone="slate">{enumLabel(SubscriptionStatus[subscription.status])}</Badge>
                                    </td>
                                    <td className="px-3 py-2 text-slate-500 dark:text-slate-400">
                                        {new Date(subscription.startDate).toLocaleDateString()}
                                    </td>
                                    <td className="px-3 py-2 text-slate-500 dark:text-slate-400">
                                        {subscription.nextRenewalDate ? new Date(subscription.nextRenewalDate).toLocaleDateString() : "-"}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>

            <div className="mt-6 rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 sm:p-8 shadow-sm">
                <h2 className="text-lg font-bold text-slate-900 dark:text-white">Payment history</h2>
                <div className="mt-4 overflow-x-auto">
                    <table className="w-full text-left text-sm">
                        <thead className="border-b border-slate-200 dark:border-slate-800 text-xs uppercase tracking-wide text-slate-400 dark:text-slate-500">
                            <tr>
                                <th className="px-3 py-2 font-semibold">Amount</th>
                                <th className="px-3 py-2 font-semibold">Method</th>
                                <th className="px-3 py-2 font-semibold">Status</th>
                                <th className="px-3 py-2 font-semibold">Paid at</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                            {payments.length === 0 && (
                                <tr>
                                    <td colSpan={4} className="px-3 py-4 text-center text-slate-400 dark:text-slate-500">
                                        No payments recorded yet.
                                    </td>
                                </tr>
                            )}
                            {payments.map((payment) => (
                                <tr key={payment.id}>
                                    <td className="px-3 py-2 font-medium text-slate-900 dark:text-white">${payment.amount.toFixed(2)}</td>
                                    <td className="px-3 py-2">{enumLabel(PaymentMethod[payment.method])}</td>
                                    <td className="px-3 py-2">
                                        <Badge tone="slate">{enumLabel(PaymentStatus[payment.status])}</Badge>
                                    </td>
                                    <td className="px-3 py-2 text-slate-500 dark:text-slate-400">
                                        {new Date(payment.paidAt).toLocaleDateString()}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>
        </AppLayout>
    );
}
