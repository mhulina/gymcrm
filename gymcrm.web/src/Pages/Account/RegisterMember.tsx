import Layout from '../../Layout';
import {FormControl, FormGroup, FormLabel} from "react-bootstrap";
import React, {useState} from "react";
import Form from "react-bootstrap/Form";
import {handleMemberRegistration} from "../../utils/MembershipApi";
import Button from "react-bootstrap/Button";
import {useNavigate} from "react-router-dom";

export enum AccountType {
    Admin = 0,
    Member = 1,
    Trainer = 2,
}

export default function RegisterMember() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const navigate = useNavigate();

    async function handleSubmit(event: { preventDefault: () => void; }) {
        event.preventDefault();
        await handleMemberRegistration(email, password, navigate);
    }
    function validateForm() {
        return email.length > 0 && password.length > 0;
    }
    
    return (
        <Layout>
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
        </Layout>
    );
}