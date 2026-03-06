import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './contexts/AuthContext';
import LoginPage from './pages/LoginPage';
import CreateOrgPage from './pages/CreateOrgPage';
import DashboardPage from './pages/DashboardPage';
import InvitationsPage from './pages/InvitationsPage';
import './App.css';

const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, loading } = useAuth();

  if (loading) return <div className="container"><div className="loading">Loading...</div></div>;
  if (!user) return <Navigate to="/login" replace />;

  return <>{children}</>;
};

const OrgRequired: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, loading } = useAuth();

  if (loading) return <div className="container"><div className="loading">Loading...</div></div>;
  if (!user) return <Navigate to="/login" replace />;

  // If user has no organisations and no pending invitations, redirect to create org
  if (user.organisations.length === 0) {
    return <Navigate to="/create-org" replace />;
  }

  return <>{children}</>;
};

const AppRoutes: React.FC = () => {
  const { user, loading } = useAuth();

  if (loading) return <div className="container"><div className="loading">Loading...</div></div>;

  return (
    <Routes>
      <Route path="/login" element={user ? <Navigate to="/dashboard" replace /> : <LoginPage />} />
      <Route path="/create-org" element={
        <ProtectedRoute><CreateOrgPage /></ProtectedRoute>
      } />
      <Route path="/invitations" element={
        <ProtectedRoute><InvitationsPage /></ProtectedRoute>
      } />
      <Route path="/dashboard" element={
        <OrgRequired><DashboardPage /></OrgRequired>
      } />
      <Route path="/" element={<Navigate to={user ? "/dashboard" : "/login"} replace />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
};

function App() {
  return (
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  );
}

export default App;
