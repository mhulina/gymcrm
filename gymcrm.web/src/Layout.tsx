import React from "react";
import logo from "./logo.svg";
import "./Layout.css";
import {LogoutButton} from "./components/LogoutButton"; // Optional: separate layout styling

interface Props {
    children: React.ReactNode;
    showLogout?: boolean;
}

const Layout = ({ children, showLogout = false }: Props) => {
    return (
        <div className="App">
            <header className="App-header">
                <img src={logo} className="App-logo" alt="logo" />
                {showLogout && <LogoutButton />}
                {children}
            </header>
        </div>
    );
};

export default Layout;
