import React, { useState } from "react";
import {Link, useNavigate} from "react-router-dom";
import {AuthCard} from "../components/AuthCard";
import {TextField} from "../../../shared/components/TextField";
import {Button} from "../../../shared/components/Button";
import {Banner} from "../../../shared/components/Banner";
import {handleLogin} from "../api/identityApi";
import {useAuth} from "../../../shared/auth/AuthContext";

export default function LoginPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);
    const navigate = useNavigate();
    const { setIsAuthenticated } = useAuth();

    function validateForm() {
        return email.length > 0 && password.length > 0;
    }

    async function handleSubmit(event: { preventDefault: () => void; }) {
        event.preventDefault();
        setError(null);
        setSubmitting(true);

        const success = await handleLogin(email, password, navigate);

        if (success) {
            setIsAuthenticated(true);
        } else {
            setError("We couldn't sign you in. Check your email and password and try again.");
        }

        setSubmitting(false);
    }

    return (
        <AuthCard
            title="Sign in"
            subtitle="Welcome back to your gym."
            footer={<>Don't have an account? <Link to="/register" className="font-medium text-emerald-600 hover:text-emerald-700 dark:text-emerald-400 dark:hover:text-emerald-300">Create one</Link></>}
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
                <Button type="submit" className="w-full" disabled={!validateForm() || submitting}>
                    {submitting ? "Signing in..." : "Sign in"}
                </Button>
            </form>
        </AuthCard>
    );
}
