import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "../shared/auth/AuthContext";
import { ThemeProvider } from "../shared/theme/ThemeContext";
import AppRoutes from "./AppRoutes";

function App() {
    return (
        <ThemeProvider>
            <AuthProvider>
                <BrowserRouter>
                    <AppRoutes />
                </BrowserRouter>
            </AuthProvider>
        </ThemeProvider>
    );
}

export default App;
