import {InputHTMLAttributes, forwardRef} from "react";

interface Props extends InputHTMLAttributes<HTMLInputElement> {
    label: string;
    error?: string;
}

export const TextField = forwardRef<HTMLInputElement, Props>(
    ({ label, error, id, className = "", ...rest }, ref) => {
        return (
            <div>
                <label htmlFor={id} className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1.5">
                    {label}
                </label>
                <input
                    ref={ref}
                    id={id}
                    className={`w-full rounded-lg border px-3 py-2 text-sm text-slate-900 dark:text-white bg-white dark:bg-slate-800 placeholder:text-slate-400 dark:placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 disabled:opacity-60 disabled:cursor-not-allowed transition-colors ${
                        error ? "border-red-400 dark:border-red-500" : "border-slate-300 dark:border-slate-700"
                    } ${className}`}
                    {...rest}
                />
                {error && <p className="mt-1.5 text-xs text-red-600 dark:text-red-400">{error}</p>}
            </div>
        );
    }
);
TextField.displayName = "TextField";
