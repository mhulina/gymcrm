import React from "react";

interface Props {
    variant: "error" | "success" | "info";
    children: React.ReactNode;
}

const styles: Record<Props["variant"], string> = {
    error: "bg-red-50 dark:bg-red-950/40 text-red-700 dark:text-red-300 border-red-200 dark:border-red-900",
    success: "bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-900",
    info: "bg-slate-50 dark:bg-slate-800/60 text-slate-600 dark:text-slate-300 border-slate-200 dark:border-slate-700",
};

export function Banner({ variant, children }: Props) {
    return (
        <div className={`mb-4 rounded-lg border px-3.5 py-2.5 text-sm ${styles[variant]}`}>
            {children}
        </div>
    );
}
