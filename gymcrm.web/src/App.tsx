import './App.css';
import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./Pages/Account/Login";
import Home from "./Pages/Home/Home";
import MemberHomePage from "./Pages/Member/MemberHomePage";
import RegisterMember from "./Pages/Account/RegisterMember";
import PublicRoute from "./utils/PublicRoute";
import PrivateRoute from "./utils/PrivateRoute";
import {AuthProvider} from "./contexts/AuthContext";

function App() {
    return (
        <AuthProvider>
          <BrowserRouter>
              <Routes>
                  <Route path="/" element={<Home />} />
                  <Route path="/login" element={
                    <PublicRoute>
                        <Login />
                    </PublicRoute>
                  } />
                  <Route path="/member/home" element={
                    <PrivateRoute>
                        <MemberHomePage />
                    </PrivateRoute>
                  } />
                  <Route path="/register" element={
                    <PublicRoute>
                        <RegisterMember />
                    </PublicRoute>
                  } />
              </Routes>
          </BrowserRouter>
        </AuthProvider>
  );
}

export default App;