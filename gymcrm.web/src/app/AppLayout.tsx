import React from "react";
import logo from "../logo.svg";
import {LogoutButton} from "../shared/components/LogoutButton";

interface Props {
    children: React.ReactNode;
    showLogout?: boolean;
}

const AppLayout = ({ children, showLogout = false }: Props) => {
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

export default AppLayout;
