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
import Profile from "./pages/Profile";
import PaymentManagement from "./pages/PaymentManagement";
import PaymentReturn from "./pages/PaymentReturn";

function App() {
    return (
        <BrowserRouter>

            <Routes>
                <Route path="/" element={<LandingPage />} />

                {/* Yoco returns to these SPA routes after checkout. */}
                <Route path="/payment/success" element={<PaymentReturn />} />
                <Route path="/payment/cancel" element={<PaymentReturn cancelled />} />

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

                {/* PROFILE ROUTE: lets users manage their own account details. */}
                <Route
                    path="/profile"
                    element={<Profile />}
                />

                {/* STAFF/ADMIN PAYMENT AND REFUND OPERATIONS. */}
                <Route
                    path="/payments"
                    element={<PaymentManagement />}
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