import React, {useState} from "react";
import {useNavigate} from "react-router-dom";
import {AuthCard} from "../components/AuthCard";
import {TextField} from "../../../shared/components/TextField";
import {Button} from "../../../shared/components/Button";
import {Banner} from "../../../shared/components/Banner";
import {changePassword} from "../api/identityApi";
import {useAuth} from "../../../shared/auth/AuthContext";

export default function ChangePasswordPage() {
    const [oldPassword, setOldPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [confirmNewPassword, setConfirmNewPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);
    const navigate = useNavigate();
    const {setMustChangePassword} = useAuth();

    const passwordsMismatch = confirmNewPassword.length > 0 && newPassword !== confirmNewPassword;

    function validateForm() {
        return oldPassword.length > 0 && newPassword.length > 0 && newPassword === confirmNewPassword;
    }

    async function handleSubmit(event: { preventDefault: () => void }) {
        event.preventDefault();
        setError(null);
        setSubmitting(true);

        const result = await changePassword(oldPassword, newPassword);

        if (result.success) {
            setMustChangePassword(false);
            navigate("/member/home", {replace: true});
        } else {
            setError(result.error ?? "We couldn't change your password. Try again.");
        }

        setSubmitting(false);
    }

    return (
        <AuthCard
            title="Set a new password"
            subtitle="Your current password was assigned by an admin - choose a new one to continue."
        >
            {error && <Banner variant="error">{error}</Banner>}
            <form onSubmit={handleSubmit} className="space-y-4">
                <TextField
                    id="oldPassword"
                    label="Current password"
                    type="password"
                    autoFocus
                    value={oldPassword}
                    onChange={(e) => setOldPassword(e.target.value)}
                />
                <TextField
                    id="newPassword"
                    label="New password"
                    type="password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                />
                <TextField
                    id="confirmNewPassword"
                    label="Confirm new password"
                    type="password"
                    value={confirmNewPassword}
                    onChange={(e) => setConfirmNewPassword(e.target.value)}
                    error={passwordsMismatch ? "Passwords don't match." : undefined}
                />
                <Button type="submit" className="w-full" disabled={!validateForm() || submitting}>
                    {submitting ? "Saving..." : "Save new password"}
                </Button>
            </form>
        </AuthCard>
    );
}
