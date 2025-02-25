import logo from './logo.svg';
import './App.css';
import { Routes, Route } from "react-router-dom";
import Login from "./containers/Login";

const Member = fetch(
    process.env.REACT_APP_MEMBERS_ENDPOINT+'GetAllUsers', {
        method: "GET", 
        mode: 'cors', 
        credentials: 'include'})
    .then((response) => { 
      return response.json();
    })
    .then((data) => { 
      console.log(data);  
      return data; 
    });

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <img src={logo} className="App-logo" alt="logo" />
        <p>
          Edit <code>src/App.js</code> and save to reload.
        </p>
        <MyButton />
        <a
          className="App-link"
          href="https://reactjs.org"
          target="_blank"
          rel="noopener noreferrer"
        >
          Learn React
        </a>
          <Routes>
              <Route path="/login" element={<Login />} />
          </Routes>
      </header>
    </div>
  );
}

console.log(process.env.REACT_APP_MEMBERS_ENDPOINT);
console.log(process.env.REACT_APP_ACCOUNTS_ENDPOINT);

function MyButton() {
  return (
      <button>I'm a button</button>
  );
}

export default App;