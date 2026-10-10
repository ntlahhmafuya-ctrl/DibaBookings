import {
    BrowserRouter,
    Routes,
    Route,
} from "react-router-dom";

import { ToastContainer } from "react-toastify";

import Login from "./pages/Login";
import Register from "./pages/Register";
import Dashboard from "./pages/dashboards/Dashboard";
import LandingPage from "./pages/LandingPage";
import PrivacyPolicy from "./pages/PrivacyPolicy";

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

                <Route
                    path="/privacy"
                    element={<PrivacyPolicy />}
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