import React from "react";
import {ThemeToggle} from "../../../shared/theme/ThemeToggle";

interface Props {
    title: string;
    subtitle?: string;
    children: React.ReactNode;
    footer?: React.ReactNode;
}

export function AuthCard({ title, subtitle, children, footer }: Props) {
    return (
        <div className="relative min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 px-4 py-12 overflow-hidden">
            <div
                aria-hidden
                className="pointer-events-none absolute -top-40 left-1/2 h-[32rem] w-[32rem] -translate-x-1/2 rounded-full bg-emerald-400/20 dark:bg-emerald-500/10 blur-3xl"
            />

            <div className="absolute top-4 right-4">
                <ThemeToggle />
            </div>

            <div className="relative w-full max-w-sm">
                <div className="mb-6 flex items-center justify-center gap-2">
                    <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-600 text-white font-bold">G</span>
                    <span className="text-lg font-bold tracking-tight text-slate-900 dark:text-white">GymCRM</span>
                </div>

                <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-8 shadow-xl shadow-slate-900/5 dark:shadow-black/20">
                    <h1 className="text-xl font-bold text-slate-900 dark:text-white text-balance">{title}</h1>
                    {subtitle && <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{subtitle}</p>}
                    <div className="mt-6">{children}</div>
                </div>

                {footer && (
                    <p className="mt-6 text-center text-sm text-slate-500 dark:text-slate-400">{footer}</p>
                )}
            </div>
        </div>
    );
}
