import React from 'react';
import ReactDOM from 'react-dom/client';
import "./styles/global.css";
import "./styles/landing.css";
import "./styles/organizer.css";
import "./styles/dashboards.css";
import "./styles/venue-map.css";
import "./styles/dashboard-responsive.css";
import "./styles/organizer-home.css";
import "leaflet/dist/leaflet.css";
import "leaflet-routing-machine/dist/leaflet-routing-machine.css";

import "bootstrap/dist/css/bootstrap.min.css";
import "react-toastify/dist/ReactToastify.css";
import App from './App';
import reportWebVitals from './reportWebVitals';

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
