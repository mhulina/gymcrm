import React, { useState } from "react";
import Form from "react-bootstrap/Form";
import Button from "react-bootstrap/Button";
import "./Login.css";

export default function Login() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [token, setToken] = useState("");

    function validateForm() {
        return email.length > 0 && password.length > 0;
    }

    function handleSubmit(event) {
        event.preventDefault();
        
        var jsonLogin = JSON.stringify({username: email.trim(), password: password});
        console.log(jsonLogin);
        
        var token = fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT+"Login",{
                headers: {"Content-Type": "application/json"},
                method: "POST",
                body: jsonLogin
            })
            .then(res => res.json())
            .then(json => {
                console.log(json);
                
                if (json) {
                    localStorage.setItem("token", json);
                }
                else{
                    localStorage.setItem("token", null);
                }
            });
    }
    return (
        <div className="Login">
            <Form onSubmit={handleSubmit}>
                <Form.Group size="lg" controlId="email">
                    <Form.Label>Email</Form.Label>
                    <Form.Control
                        autoFocus
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                    />
                </Form.Group>
                <Form.Group size="lg" controlId="password">
                    <Form.Label>Password</Form.Label>
                    <Form.Control 
                        type="password" 
                        value={password} 
                        onChange={(e) => setPassword(e.target.value)}
                    />
                </Form.Group>
                <Button block size="lg" type="submit" disabled={!validateForm()}>
                    Login
                </Button>
            </Form>
        </div>
    );
}