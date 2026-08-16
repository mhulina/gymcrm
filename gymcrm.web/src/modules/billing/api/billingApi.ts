import {axios} from "./billingHttpClient";
import {Subscription} from "../types/subscription";
import {InsertSubscription} from "../types/insertSubscription";
import {Payment} from "../types/payment";
import {InsertPayment} from "../types/insertPayment";
import {extractErrorMessage} from "../../../shared/api/extractErrorMessage";

export async function fetchSubscriptionById(subscriptionId: string): Promise<Subscription | null> {
    try {
        const response = await axios.get<Subscription>(`Subscriptions/GetSubscriptionById/${subscriptionId}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching subscription: ", error);
        return null;
    }
}

export async function fetchActiveSubscriptionForMember(memberAccountGuid: string): Promise<Subscription | null> {
    try {
        const response = await axios.get<Subscription | null>(`Subscriptions/GetActiveSubscriptionForMember/${memberAccountGuid}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching active subscription: ", error);
        return null;
    }
}

export async function fetchSubscriptionsForMember(memberAccountGuid: string): Promise<Subscription[]> {
    try {
        const response = await axios.get<Subscription[]>(`Subscriptions/GetSubscriptionsForMember/${memberAccountGuid}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching subscription history: ", error);
        return [];
    }
}

export async function createSubscription(insert: InsertSubscription): Promise<{ success: boolean; error?: string }> {
    try {
        await axios.post("Subscriptions/CreateSubscription", insert);
        return { success: true };
    } catch (error) {
        console.error("Error creating subscription: ", error);
        return { success: false, error: extractErrorMessage(error, "We couldn't create that subscription.") };
    }
}

export async function renewSubscription(subscriptionId: string): Promise<{ success: boolean; error?: string }> {
    try {
        await axios.put(`Subscriptions/RenewSubscription/${subscriptionId}`);
        return { success: true };
    } catch (error) {
        console.error("Error renewing subscription: ", error);
        return { success: false, error: extractErrorMessage(error, "We couldn't renew this subscription.") };
    }
}

export async function cancelSubscription(subscriptionId: string): Promise<{ success: boolean; error?: string }> {
    try {
        await axios.put(`Subscriptions/CancelSubscription/${subscriptionId}`);
        return { success: true };
    } catch (error) {
        console.error("Error cancelling subscription: ", error);
        return { success: false, error: extractErrorMessage(error, "We couldn't cancel this subscription.") };
    }
}

export async function markSubscriptionPastDue(subscriptionId: string): Promise<{ success: boolean; error?: string }> {
    try {
        await axios.put(`Subscriptions/MarkSubscriptionPastDue/${subscriptionId}`);
        return { success: true };
    } catch (error) {
        console.error("Error marking subscription past due: ", error);
        return { success: false, error: extractErrorMessage(error, "We couldn't update this subscription.") };
    }
}

export async function fetchPaymentById(paymentId: string): Promise<Payment | null> {
    try {
        const response = await axios.get<Payment>(`Payments/GetPaymentById/${paymentId}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching payment: ", error);
        return null;
    }
}

export async function fetchPaymentsForSubscription(subscriptionId: string): Promise<Payment[]> {
    try {
        const response = await axios.get<Payment[]>(`Payments/GetPaymentsForSubscription/${subscriptionId}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching payments: ", error);
        return [];
    }
}

export async function fetchPaymentsForMember(memberAccountGuid: string): Promise<Payment[]> {
    try {
        const response = await axios.get<Payment[]>(`Payments/GetPaymentsForMember/${memberAccountGuid}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching payment history: ", error);
        return [];
    }
}

export async function recordPayment(insert: InsertPayment): Promise<{ success: boolean; error?: string }> {
    try {
        await axios.post("Payments/RecordPayment", insert);
        return { success: true };
    } catch (error) {
        console.error("Error recording payment: ", error);
        return { success: false, error: extractErrorMessage(error, "We couldn't record that payment.") };
    }
}

export async function refundPayment(paymentId: string): Promise<{ success: boolean; error?: string }> {
    try {
        await axios.put(`Payments/RefundPayment/${paymentId}`);
        return { success: true };
    } catch (error) {
        console.error("Error refunding payment: ", error);
        return { success: false, error: extractErrorMessage(error, "We couldn't refund this payment.") };
    }
}
