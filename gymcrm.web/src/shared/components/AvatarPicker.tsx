import {useRef} from "react";
import {UnsavedFieldNote} from "./UnsavedFieldNote";

interface Props {
    initials: string;
    previewUrl: string | null;
    onFileSelected: (file: File, previewUrl: string) => void;
}

// Local preview only - there is no backend field or endpoint to persist a photo
// to yet, so this never attempts an upload. See UnsavedFieldNote below it.
export function AvatarPicker({ initials, previewUrl, onFileSelected }: Props) {
    const inputRef = useRef<HTMLInputElement>(null);

    function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
        const file = event.target.files?.[0];
        if (!file) {
            return;
        }

        const reader = new FileReader();
        reader.onload = () => {
            if (typeof reader.result === "string") {
                onFileSelected(file, reader.result);
            }
        };
        reader.readAsDataURL(file);
    }

    return (
        <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1.5">
                Photo
            </label>
            <div className="flex items-center gap-4">
                <button
                    type="button"
                    onClick={() => inputRef.current?.click()}
                    className="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-full bg-emerald-600 text-lg font-bold text-white transition-opacity hover:opacity-90"
                    aria-label="Choose a photo"
                >
                    {previewUrl ? (
                        <img src={previewUrl} alt="" className="h-full w-full object-cover" />
                    ) : (
                        initials
                    )}
                </button>
                <button
                    type="button"
                    onClick={() => inputRef.current?.click()}
                    className="rounded-lg border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm font-medium text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors"
                >
                    {previewUrl ? "Change photo" : "Upload photo"}
                </button>
                <input
                    ref={inputRef}
                    type="file"
                    accept="image/*"
                    onChange={handleChange}
                    className="hidden"
                />
            </div>
            <UnsavedFieldNote />
        </div>
    );
}
