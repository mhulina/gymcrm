import {useEffect, useState} from "react";
import {Link, useParams} from "react-router-dom";
import AppLayout from "../../../app/AppLayout";
import {Badge} from "../../../shared/components/Badge";
import {Banner} from "../../../shared/components/Banner";
import {Button} from "../../../shared/components/Button";
import {TextField} from "../../../shared/components/TextField";
import {SelectField} from "../../../shared/components/SelectField";
import {enumLabel} from "../../../shared/utils/mapper";
import {fetchMemberByGuid} from "../../identity/api/identityApi";
import {Member} from "../../identity/types/member";
import {fullName} from "../../identity/utils/memberDisplay";
import {SubscriptionPlanTypeDropdown} from "../components/SubscriptionPlanTypeDropdown";
import {PaymentMethodDropdown} from "../components/PaymentMethodDropdown";
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
    createSubscription,
    renewSubscription,
    cancelSubscription,
    recordPayment,
    refundPayment
} from "../api/billingApi";

export default function AdminMemberBillingPage() {
    const { guid } = useParams<{ guid: string }>();
    const [member, setMember] = useState<Member | null>(null);
    const [activeSubscription, setActiveSubscription] = useState<Subscription | null>(null);
    const [subscriptionHistory, setSubscriptionHistory] = useState<Subscription[]>([]);
    const [payments, setPayments] = useState<Payment[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    const [newPlanType, setNewPlanType] = useState(SubscriptionPlanType.Monthly);

    const [paymentSubscriptionId, setPaymentSubscriptionId] = useState("");
    const [paymentAmount, setPaymentAmount] = useState("");
    const [paymentMethod, setPaymentMethod] = useState(PaymentMethod.Cash);
    const [paymentStatus, setPaymentStatus] = useState(PaymentStatus.Succeeded);
    const [paymentReference, setPaymentReference] = useState("");

    function load() {
        if (!guid) {
            return;
        }

        setLoading(true);

        Promise.all([
            fetchMemberByGuid(guid),
            fetchActiveSubscriptionForMember(guid),
            fetchSubscriptionsForMember(guid),
            fetchPaymentsForMember(guid)
        ]).then(([memberResult, activeResult, historyResult, paymentsResult]) => {
            setMember(memberResult);
            setActiveSubscription(activeResult);
            setSubscriptionHistory(historyResult);
            setPayments(paymentsResult);

            if (activeResult) {
                setPaymentSubscriptionId(activeResult.id);
            } else if (historyResult.length > 0) {
                setPaymentSubscriptionId(historyResult[0].id);
            }

            setLoading(false);
        });
    }

    // eslint-disable-next-line react-hooks/exhaustive-deps
    useEffect(load, [guid]);

    async function handleCreateSubscription() {
        if (!guid) {
            return;
        }

        setError(null);
        setSubmitting(true);

        const result = await createSubscription({ memberAccountGuid: guid, planType: newPlanType });

        if (result.success) {
            load();
        } else {
            setError(result.error ?? "We couldn't create that subscription.");
        }

        setSubmitting(false);
    }

    async function handleRenew() {
        if (!activeSubscription) {
            return;
        }

        setError(null);
        setSubmitting(true);

        const result = await renewSubscription(activeSubscription.id);

        if (result.success) {
            load();
        } else {
            setError(result.error ?? "We couldn't renew this subscription.");
        }

        setSubmitting(false);
    }

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

    async function handleRecordPayment(event: { preventDefault: () => void }) {
        event.preventDefault();

        if (!paymentSubscriptionId || !paymentAmount) {
            return;
        }

        setError(null);
        setSubmitting(true);

        const result = await recordPayment({
            subscriptionId: paymentSubscriptionId,
            amount: Number(paymentAmount),
            method: paymentMethod,
            status: paymentStatus,
            externalReference: paymentReference || undefined
        });

        if (result.success) {
            setPaymentAmount("");
            setPaymentReference("");
            load();
        } else {
            setError(result.error ?? "We couldn't record that payment.");
        }

        setSubmitting(false);
    }

    async function handleRefund(paymentId: string) {
        setError(null);
        setSubmitting(true);

        const result = await refundPayment(paymentId);

        if (result.success) {
            load();
        } else {
            setError(result.error ?? "We couldn't refund this payment.");
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
                <Link to="/admin/members" className="text-sm font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300">
                    &larr; Back to members
                </Link>
                <p className="mt-4 text-sm text-red-600 dark:text-red-400">We couldn't find that member.</p>
            </AppLayout>
        );
    }

    return (
        <AppLayout showLogout>
            <Link to="/admin/members" className="text-sm font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300">
                &larr; Back to members
            </Link>
            <h1 className="mt-4 mb-6 text-xl font-bold text-slate-900 dark:text-white">
                Billing for {fullName(member) || member.email}
            </h1>

            {error && <Banner variant="error">{error}</Banner>}

            <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 sm:p-8 shadow-sm">
                <h2 className="text-lg font-bold text-slate-900 dark:text-white">Current subscription</h2>

                {activeSubscription ? (
                    <div className="mt-4">
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
                        <div className="mt-4 flex gap-2">
                            <Button variant="secondary" disabled={submitting} onClick={handleRenew}>Renew</Button>
                            <Button variant="secondary" disabled={submitting} onClick={handleCancel}>Cancel</Button>
                        </div>
                    </div>
                ) : (
                    <div className="mt-4">
                        <p className="text-sm text-slate-500 dark:text-slate-400">No active subscription.</p>
                        <div className="mt-4 flex flex-wrap items-end gap-3">
                            <SubscriptionPlanTypeDropdown id="newPlanType" label="Plan" value={newPlanType} onChange={setNewPlanType} />
                            <Button disabled={submitting} onClick={handleCreateSubscription}>Create subscription</Button>
                        </div>
                    </div>
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
                <h2 className="text-lg font-bold text-slate-900 dark:text-white">Record a payment</h2>

                {subscriptionHistory.length === 0 ? (
                    <p className="mt-4 text-sm text-slate-500 dark:text-slate-400">
                        This member has no subscription to record a payment against yet.
                    </p>
                ) : (
                    <form onSubmit={handleRecordPayment} className="mt-4 space-y-4">
                        <SelectField
                            id="paymentSubscriptionId"
                            label="Subscription"
                            value={paymentSubscriptionId}
                            onChange={(e) => setPaymentSubscriptionId(e.target.value)}
                        >
                            {subscriptionHistory.map((subscription) => (
                                <option key={subscription.id} value={subscription.id}>
                                    {enumLabel(SubscriptionPlanType[subscription.planType])} - {enumLabel(SubscriptionStatus[subscription.status])} ({new Date(subscription.startDate).toLocaleDateString()})
                                </option>
                            ))}
                        </SelectField>
                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                            <TextField
                                id="paymentAmount"
                                label="Amount"
                                type="number"
                                min={0}
                                step="0.01"
                                required
                                value={paymentAmount}
                                onChange={(e) => setPaymentAmount(e.target.value)}
                            />
                            <PaymentMethodDropdown id="paymentMethod" label="Method" value={paymentMethod} onChange={setPaymentMethod} />
                            <SelectField
                                id="paymentStatus"
                                label="Status"
                                value={paymentStatus}
                                onChange={(e) => setPaymentStatus(Number(e.target.value))}
                            >
                                <option value={PaymentStatus.Succeeded}>Succeeded</option>
                                <option value={PaymentStatus.Failed}>Failed</option>
                            </SelectField>
                        </div>
                        <TextField
                            id="paymentReference"
                            label="External reference (optional)"
                            value={paymentReference}
                            onChange={(e) => setPaymentReference(e.target.value)}
                        />
                        <Button type="submit" disabled={submitting}>Record payment</Button>
                    </form>
                )}
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
                                <th className="px-3 py-2" />
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                            {payments.length === 0 && (
                                <tr>
                                    <td colSpan={5} className="px-3 py-4 text-center text-slate-400 dark:text-slate-500">
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
                                    <td className="px-3 py-2 text-right">
                                        {payment.status !== PaymentStatus.Refunded && (
                                            <Button variant="ghost" disabled={submitting} onClick={() => handleRefund(payment.id)}>
                                                Refund
                                            </Button>
                                        )}
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
