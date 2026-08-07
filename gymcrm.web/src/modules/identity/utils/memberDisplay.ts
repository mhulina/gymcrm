import {Member} from "../types/member";

export function initials(member: Member) {
    const first = member.firstName?.[0] ?? member.email[0];
    const last = member.lastName?.[0] ?? "";
    return (first + last).toUpperCase();
}

export function fullName(member: Member) {
    const parts = [member.firstName, member.lastName].filter(Boolean);
    return parts.length > 0 ? parts.join(" ") : member.email;
}
