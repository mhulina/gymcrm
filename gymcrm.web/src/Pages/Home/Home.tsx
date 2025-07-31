import logo from '../../logo.svg';
import {fetchUserInfoByGuid} from "../../utils/MembershipApi";

function Home() {
    return (
        <div className="App">
            <header className="App-header">
                <img src={logo} className="App-logo" alt="logo"/>
                <p>Edit <code>src/Home.tsx</code> and save to reload.</p>
                <MyButton />
                <a
                    className="App-link"
                    href="https://reactjs.org"
                    target="_blank"
                    rel="noopener noreferrer"
                >
                    Learn React
                </a>
            </header>
        </div>
    );
}

function MyButton() {
    const handleClick = () => {
        const userData = fetchUserInfoByGuid();
        console.log(userData);
    };

    return <button onClick={handleClick}>I'm a button</button>;
}

export default Home;