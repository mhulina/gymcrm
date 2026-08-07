import React, {useState} from "react";
import {Link, useNavigate} from "react-router-dom";
import {AuthCard} from "../components/AuthCard";
import {TextField} from "../../../shared/components/TextField";
import {Button} from "../../../shared/components/Button";
import {Banner} from "../../../shared/components/Banner";
import {handleMemberRegistration} from "../api/identityApi";
import {useAuth} from "../../../shared/auth/AuthContext";

export default function RegisterMemberPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);
    const navigate = useNavigate();
    const { setIsAuthenticated } = useAuth();

    const passwordsMismatch = confirmPassword.length > 0 && password !== confirmPassword;

    function validateForm() {
        return email.length > 0 && password.length > 0 && password === confirmPassword;
    }

    async function handleSubmit(event: { preventDefault: () => void; }) {
        event.preventDefault();
        setError(null);
        setSubmitting(true);

        const success = await handleMemberRegistration(email, password, navigate);
        setIsAuthenticated(success);

        if (!success) {
            setError("We couldn't create your account. That email may already be registered.");
        }

        setSubmitting(false);
    }

    return (
        <AuthCard
            title="Create your account"
            subtitle="Sign up as a gym member."
            footer={<>Already have an account? <Link to="/login" className="font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300">Sign in</Link></>}
        >
            {error && <Banner variant="error">{error}</Banner>}
            <form onSubmit={handleSubmit} className="space-y-4">
                <TextField
                    id="email"
                    label="Email"
                    type="email"
                    autoFocus
                    placeholder="name@example.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                />
                <TextField
                    id="password"
                    label="Password"
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />
                <TextField
                    id="confirmPassword"
                    label="Confirm password"
                    type="password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    error={passwordsMismatch ? "Passwords don't match." : undefined}
                />
                <Button type="submit" className="w-full" disabled={!validateForm() || submitting}>
                    {submitting ? "Creating account..." : "Create account"}
                </Button>
            </form>
        </AuthCard>
    );
}
