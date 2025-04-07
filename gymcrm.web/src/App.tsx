import './App.css';
import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./Pages/Account/Login";
import Home from "./Pages/Home/Home";
import MemberHomePage from "./Pages/Member/MemberHomePage";
import RegisterMember from "./Pages/Account/RegisterMember";

function App() {
  return (
      <BrowserRouter>
          <Routes>
              <Route path="/" element={<Home />} />
              <Route path="/login" element={<Login />} />
              <Route path="/member/home" element={<MemberHomePage />} />
              <Route path="/register" element={<RegisterMember />} />
          </Routes>
      </BrowserRouter>
  );
}

console.log(process.env.REACT_APP_MEMBERS_ENDPOINT);
console.log(process.env.REACT_APP_ACCOUNTS_ENDPOINT);

export default App;