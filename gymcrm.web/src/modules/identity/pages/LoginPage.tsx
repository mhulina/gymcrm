import React, { useState } from "react";
import Form from "react-bootstrap/Form";
import Button from "react-bootstrap/Button";
import {useNavigate} from "react-router-dom";
import AppLayout from "../../../app/AppLayout";
import {handleLogin} from "../api/identityApi";
import {useAuth} from "../../../shared/auth/AuthContext";

export default function LoginPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const navigate = useNavigate();
    const { setIsAuthenticated } = useAuth();

    function validateForm() {
        return email.length > 0 && password.length > 0;
    }

    async function handleSubmit(event: { preventDefault: () => void; }) {
        event.preventDefault();
        const success = await handleLogin(email, password, navigate);
        
        if (success) {
            setIsAuthenticated(true);
        }
    }
    
    return (
        <AppLayout>
        <div className="Login">
            <Form onSubmit={handleSubmit}>
                <Form.Group controlId="email">
                    <Form.Label>Email</Form.Label>
                    <Form.Control
                        autoFocus
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                    />
                </Form.Group>
                <Form.Group  controlId="password">
                    <Form.Label>Password</Form.Label>
                    <Form.Control
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                    />
                </Form.Group>
                <Button size="lg" type="submit" disabled={!validateForm()}>
                    Login
                </Button>
            </Form>
        </div>
        </AppLayout>
    );
}