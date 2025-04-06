import './App.css';
import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./components/Login";
import Home from "./components/Home";
import MemberHomePage from "./components/Members/MemberHomePage";

function App() {
  return (
      <BrowserRouter>
          <Routes>
              <Route path="/" element={<Home />} />
              <Route path="/login" element={<Login />} />
              <Route path="/member/home" element={<MemberHomePage />} />
          </Routes>
      </BrowserRouter>
  );
}

console.log(process.env.REACT_APP_MEMBERS_ENDPOINT);
console.log(process.env.REACT_APP_ACCOUNTS_ENDPOINT);

export default App;