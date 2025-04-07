import React from "react";
import logo from "./logo.svg";
import "./Layout.css"; // Optional: separate layout styling

interface Props {
    children: React.ReactNode;
}

const Layout = ({ children }: Props) => {
    return (
        <div className="App">
            <header className="App-header">
                <img src={logo} className="App-logo" alt="logo" />
                {children}
            </header>
        </div>
    );
};

export default Layout;
