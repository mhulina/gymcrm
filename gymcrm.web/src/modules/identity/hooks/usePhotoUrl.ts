import {useEffect, useState} from "react";
import {fetchMemberPhotoUrl} from "../api/identityApi";

// Loads a member's photo as a local object URL (see fetchMemberPhotoUrl for why this can't
// just be an <img src="...GetPhoto/...">), and revokes it whenever it's replaced or this
// component unmounts, so navigating around the app doesn't leak blob URLs.
export function usePhotoUrl(accountGuid: string | undefined, hasPhoto: boolean, refreshToken = 0): string | null {
    const [photoUrl, setPhotoUrl] = useState<string | null>(null);

    useEffect(() => {
        let objectUrl: string | null = null;
        let cancelled = false;

        setPhotoUrl(null);

        if (!accountGuid || !hasPhoto) {
            return;
        }

        fetchMemberPhotoUrl(accountGuid).then((url) => {
            if (cancelled) {
                if (url) URL.revokeObjectURL(url);
                return;
            }
            objectUrl = url;
            setPhotoUrl(url);
        });

        return () => {
            cancelled = true;
            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
            }
        };
    }, [accountGuid, hasPhoto, refreshToken]);

    return photoUrl;
}
