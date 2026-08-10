import {useState} from "react";
import {useNavigate} from "react-router-dom";
import {AuthCard} from "../components/AuthCard";
import {TextField} from "../../../shared/components/TextField";
import {Button} from "../../../shared/components/Button";
import {Banner} from "../../../shared/components/Banner";
import {handleLogin, setupAdminAccount} from "../api/identityApi";
import {useAuth} from "../../../shared/auth/AuthContext";

export default function AdminSetupPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);
    const navigate = useNavigate();
    const { setIsAuthenticated, refreshHasAdminAccount } = useAuth();

    const passwordsMismatch = confirmPassword.length > 0 && password !== confirmPassword;

    function validateForm() {
        return email.length > 0 && password.length > 0 && password === confirmPassword;
    }

    async function handleSubmit(event: { preventDefault: () => void }) {
        event.preventDefault();
        setError(null);
        setSubmitting(true);

        const result = await setupAdminAccount(email, password);

        if (!result.success) {
            setError(result.error ?? "We couldn't create the admin account.");
            setSubmitting(false);
            return;
        }

        // Must resolve before navigating - PrivateRoute/RootRedirect would otherwise still
        // see hasAdminAccount as false (stale) and bounce straight back to /setup.
        await refreshHasAdminAccount();

        const loginResult = await handleLogin(email, password, navigate);
        setIsAuthenticated(loginResult.success);
        setSubmitting(false);
    }

    return (
        <AuthCard
            title="Set up your admin account"
            subtitle="This is a one-time step - it only appears until the first admin account exists."
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
                    {submitting ? "Creating admin account..." : "Create admin account"}
                </Button>
            </form>
        </AuthCard>
    );
}
