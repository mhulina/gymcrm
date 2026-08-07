import {ChangeEvent, useRef, useState} from "react";
import {deleteMemberPhoto, uploadMemberPhoto} from "../api/identityApi";

// Owns the file-input plumbing and upload/delete calls shared by every photo-change
// surface (the Edit Profile AvatarPicker and the dashboard's quick-change button) - both
// upload immediately on file pick (never gated behind a form's Save button).
// onChanged receives the new hasPhoto value so callers can update their own local copy
// of the member record without a full refetch.
export function useAvatarUpload(accountGuid: string | undefined, onChanged: (hasPhoto: boolean) => void) {
    const inputRef = useRef<HTMLInputElement>(null);
    const [uploading, setUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    function triggerPick() {
        setError(null);
        inputRef.current?.click();
    }

    async function handleFileSelected(event: ChangeEvent<HTMLInputElement>) {
        const file = event.target.files?.[0];
        event.target.value = ""; // allow picking the same file again later
        if (!file || !accountGuid) {
            return;
        }

        setUploading(true);
        setError(null);
        const result = await uploadMemberPhoto(accountGuid, file);
        setUploading(false);

        if (result.success) {
            onChanged(true);
        } else {
            setError(result.error ?? "We couldn't upload this photo.");
        }
    }

    async function removePhoto() {
        if (!accountGuid) {
            return;
        }

        setUploading(true);
        setError(null);
        const result = await deleteMemberPhoto(accountGuid);
        setUploading(false);

        if (result.success) {
            onChanged(false);
        } else {
            setError(result.error ?? "We couldn't remove this photo.");
        }
    }

    return {
        uploading,
        error,
        triggerPick,
        removePhoto,
        inputRef,
        onFileSelected: handleFileSelected,
    };
}
