import {SelectHTMLAttributes, forwardRef} from "react";

interface Props extends SelectHTMLAttributes<HTMLSelectElement> {
    label: string;
}

export const SelectField = forwardRef<HTMLSelectElement, Props>(
    ({ label, id, className = "", children, ...rest }, ref) => {
        return (
            <div>
                <label htmlFor={id} className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1.5">
                    {label}
                </label>
                <select
                    ref={ref}
                    id={id}
                    className={`w-full rounded-lg border border-slate-300 dark:border-slate-700 px-3 py-2 text-sm text-slate-900 dark:text-white bg-white dark:bg-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 disabled:opacity-60 disabled:cursor-not-allowed transition-colors ${className}`}
                    {...rest}
                >
                    {children}
                </select>
            </div>
        );
    }
);
SelectField.displayName = "SelectField";
