import React from "react";
import {Link} from "react-router-dom";
import {LogoutButton} from "../shared/components/LogoutButton";
import {ThemeToggle} from "../shared/theme/ThemeToggle";

interface Props {
    children: React.ReactNode;
    showLogout?: boolean;
}

const AppLayout = ({ children, showLogout = false }: Props) => {
    return (
        <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
            <header className="sticky top-0 z-10 border-b border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/80 backdrop-blur">
                <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3 sm:px-6">
                    <Link to="/member/home" className="flex items-center gap-2">
                        <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-600 text-sm font-bold text-white">G</span>
                        <span className="text-base font-bold tracking-tight text-slate-900 dark:text-white">GymCRM</span>
                    </Link>
                    <div className="flex items-center gap-2">
                        <ThemeToggle />
                        {showLogout && <LogoutButton />}
                    </div>
                </div>
            </header>
            <main className="mx-auto max-w-5xl px-4 py-8 sm:px-6">
                {children}
            </main>
        </div>
    );
};

export default AppLayout;
