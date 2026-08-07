import React from "react";

interface Props {
    children: React.ReactNode;
    tone?: "emerald" | "slate";
}

const tones: Record<NonNullable<Props["tone"]>, string> = {
    emerald: "bg-emerald-50 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300",
    slate: "bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300",
};

export function Badge({ children, tone = "emerald" }: Props) {
    return (
        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${tones[tone]}`}>
            {children}
        </span>
    );
}
