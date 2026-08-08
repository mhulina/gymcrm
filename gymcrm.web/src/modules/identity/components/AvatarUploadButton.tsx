import {useState} from "react";
import {usePhotoUrl} from "../hooks/usePhotoUrl";
import {useAvatarUpload} from "../hooks/useAvatarUpload";

interface Props {
    accountGuid: string;
    initials: string;
    hasPhoto: boolean;
    onPhotoChanged: () => void;
}

// Compact quick-change avatar for the dashboard header - lets a member change their photo
// without navigating into Edit Profile. Same underlying hooks as AvatarPicker, just a
// smaller, icon-only affordance instead of a labeled upload button.
export function AvatarUploadButton({ accountGuid, initials, hasPhoto, onPhotoChanged }: Props) {
    // Local bump, same reason as AvatarPicker: replacing an existing photo leaves
    // hasPhoto unchanged (still true), so the parent's refetch alone won't pick up
    // the new bytes without this.
    const [refreshToken, setRefreshToken] = useState(0);
    const photoUrl = usePhotoUrl(accountGuid, hasPhoto, refreshToken);
    const { uploading, error, triggerPick, inputRef, onFileSelected } = useAvatarUpload(accountGuid, () => {
        setRefreshToken((t) => t + 1);
        onPhotoChanged();
    });

    return (
        <div className="shrink-0">
            <button
                type="button"
                onClick={triggerPick}
                disabled={uploading}
                aria-label={photoUrl ? "Change your photo" : "Upload a photo"}
                className="group relative flex h-14 w-14 items-center justify-center overflow-hidden rounded-full bg-emerald-600 text-lg font-bold text-white transition-opacity hover:opacity-90 disabled:opacity-60"
            >
                {photoUrl ? (
                    <img src={photoUrl} alt="" className="h-full w-full object-cover" />
                ) : (
                    initials
                )}
                {!uploading && (
                    <span className="absolute inset-0 flex items-center justify-center bg-black/50 text-[10px] font-semibold opacity-0 transition-opacity group-hover:opacity-100">
                        Change
                    </span>
                )}
                {uploading && (
                    <span className="absolute inset-0 flex items-center justify-center bg-black/50">
                        <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                    </span>
                )}
            </button>
            <input
                ref={inputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                onChange={onFileSelected}
                className="hidden"
            />
            {error && <p className="mt-1 max-w-14 text-[10px] leading-tight text-red-600 dark:text-red-400">{error}</p>}
        </div>
    );
}
