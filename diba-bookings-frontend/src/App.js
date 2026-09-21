import {
    BrowserRouter,
    Routes,
    Route,
} from "react-router-dom";

import { ToastContainer } from "react-toastify";
import "./App.css";

import Login from "./pages/Login";
import Register from "./pages/Register";
import Dashboard from "./pages/Dashboard";
import LandingPage from "./pages/LandingPage";

function App() {
    return (
        <BrowserRouter>

            <Routes>
                <Route path="/" element={<LandingPage />} />

                <Route
                    path="/login"
                    element={<Login />}
                />

                <Route
                    path="/register"
                    element={<Register />}
                />
                
                <Route
                    path="/dashboard"
                    element={<Dashboard />}
                />
            </Routes>

            <ToastContainer
                position="top-right"
                autoClose={3000}
            />

        </BrowserRouter>
    );
}

export default App;