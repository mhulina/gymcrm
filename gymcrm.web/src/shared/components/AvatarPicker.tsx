import {useState} from "react";
import {usePhotoUrl} from "../../modules/identity/hooks/usePhotoUrl";
import {useAvatarUpload} from "../../modules/identity/hooks/useAvatarUpload";

interface Props {
    accountGuid: string;
    initials: string;
    hasPhoto: boolean;
    onPhotoChanged: (hasPhoto: boolean) => void;
}

// Uploads/removes immediately on pick - never gated behind the surrounding form's Save
// button. `onPhotoChanged` lets the caller update its own copy of the member record's
// hasPhoto flag, while the local refreshToken here covers the "replaced an existing
// photo" case, where hasPhoto stays true but the bytes changed.
export function AvatarPicker({ accountGuid, initials, hasPhoto, onPhotoChanged }: Props) {
    const [refreshToken, setRefreshToken] = useState(0);
    const photoUrl = usePhotoUrl(accountGuid, hasPhoto, refreshToken);
    const { uploading, error, triggerPick, removePhoto, inputRef, onFileSelected } = useAvatarUpload(accountGuid, (newHasPhoto) => {
        setRefreshToken((t) => t + 1);
        onPhotoChanged(newHasPhoto);
    });

    return (
        <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1.5">
                Photo
            </label>
            <div className="flex items-center gap-4">
                <button
                    type="button"
                    onClick={triggerPick}
                    disabled={uploading}
                    className="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-full bg-emerald-600 text-lg font-bold text-white transition-opacity hover:opacity-90 disabled:opacity-60"
                    aria-label="Choose a photo"
                >
                    {photoUrl ? (
                        <img src={photoUrl} alt="" className="h-full w-full object-cover" />
                    ) : (
                        initials
                    )}
                </button>
                <button
                    type="button"
                    onClick={triggerPick}
                    disabled={uploading}
                    className="rounded-lg border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm font-medium text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors disabled:opacity-60"
                >
                    {uploading ? "Uploading..." : photoUrl ? "Change photo" : "Upload photo"}
                </button>
                {photoUrl && !uploading && (
                    <button
                        type="button"
                        onClick={removePhoto}
                        className="text-sm font-medium text-red-600 dark:text-red-400 hover:underline"
                    >
                        Remove
                    </button>
                )}
                <input
                    ref={inputRef}
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    onChange={onFileSelected}
                    className="hidden"
                />
            </div>
            {error && <p className="mt-1.5 text-xs text-red-600 dark:text-red-400">{error}</p>}
        </div>
    );
}
