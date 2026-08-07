import AppLayout from "../../../app/AppLayout";
import {FormControl, FormGroup, FormLabel} from "react-bootstrap";
import React, {useState} from "react";
import Form from "react-bootstrap/Form";
import {handleMemberRegistration} from "../api/identityApi";
import Button from "react-bootstrap/Button";
import {useNavigate} from "react-router-dom";
import {useAuth} from "../../../shared/auth/AuthContext";

export default function RegisterMemberPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const navigate = useNavigate();
    const { setIsAuthenticated } = useAuth();

    async function handleSubmit(event: { preventDefault: () => void; }) {
        event.preventDefault();
        const success = await handleMemberRegistration(email, password, navigate);
        setIsAuthenticated(success);
    }
    function validateForm() {
        return email.length > 0 && password.length > 0;
    }
    
    return (
        <AppLayout>
            <Form onSubmit={handleSubmit}>
                <FormGroup className="email">
                    <FormLabel id="email">E-mail: </FormLabel>
                    <FormControl
                        autoFocus
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)} />
                </FormGroup>
                <Form.Group  controlId="password">
                    <Form.Label>Password</Form.Label>
                    <Form.Control
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                    />
                </Form.Group>
                <Button size="lg" type="submit" disabled={!validateForm()}>
                    Register
                </Button>
            </Form>
        </AppLayout>
    );
}