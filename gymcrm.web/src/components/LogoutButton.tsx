import { useNavigate } from "react-router-dom";
import { handleLogout } from "../utils/MembershipApi";
import Button from "react-bootstrap/Button";
import {useAuth} from "../contexts/AuthContext";

export function LogoutButton() {
    const navigate = useNavigate();
    const { setIsAuthenticated } = useAuth();

    const onLogout = async () => {
        await handleLogout(navigate, setIsAuthenticated);
    };

    return (
        <Button variant="outline-danger" onClick={onLogout}>
            Logout
        </Button>
    );
}