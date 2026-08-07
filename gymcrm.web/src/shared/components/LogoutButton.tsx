import { useNavigate } from "react-router-dom";
import { handleLogout } from "../../modules/identity/api/identityApi";
import {useAuth} from "../auth/AuthContext";
import {Button} from "./Button";

export function LogoutButton() {
    const navigate = useNavigate();
    const { setIsAuthenticated } = useAuth();

    const onLogout = async () => {
        await handleLogout(navigate, setIsAuthenticated);
    };

    return (
        <Button variant="secondary" onClick={onLogout}>
            Logout
        </Button>
    );
}
