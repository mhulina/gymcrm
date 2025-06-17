import './App.css';
import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./Pages/Account/Login";
import Home from "./Pages/Home/Home";
import MemberHomePage from "./Pages/Member/MemberHomePage";
import RegisterMember from "./Pages/Account/RegisterMember";
import PublicRoute from "./utils/PublicRoute";
import PrivateRoute from "./utils/PrivateRoute";
import {clearToken, isTokenValid} from "./utils/auth";
import {useEffect} from "react";

function App() {
    useEffect(() => {
        const token = localStorage.getItem("token");
        if (token && !isTokenValid(token)) {
            console.log("Token expired. Clearing it.");
            clearToken();
        }
    }, []);
    
    return (
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
  );
}

console.log(process.env.REACT_APP_MEMBERS_ENDPOINT);
console.log(process.env.REACT_APP_ACCOUNTS_ENDPOINT);

export default App;