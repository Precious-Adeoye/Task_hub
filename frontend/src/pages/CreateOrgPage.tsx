import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { organisations } from '../services';

const CreateOrgPage: React.FC = () => {
  const { user, refreshUser, logout } = useAuth();
  const navigate = useNavigate();
  const [orgName, setOrgName] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!orgName.trim()) return;
    setError('');
    setLoading(true);

    try {
      const response = await organisations.create({ name: orgName });
      localStorage.setItem('currentOrganisationId', response.data.id);
      await refreshUser();
      navigate('/dashboard');
    } catch (err: any) {
      const data = err.response?.data;
      setError(data?.detail || data?.error || 'Failed to create organisation');
    } finally {
      setLoading(false);
    }
  };

  const hasPendingInvitations = user && user.pendingInvitationCount > 0;

  return (
    <div className="container" style={{ maxWidth: '500px' }}>
      <h1>Welcome to TaskHub</h1>
      <p style={{ color: '#666', margin: '10px 0 20px' }}>
        You don't belong to any organisation yet. Create one to get started, or check your invitations.
      </p>

      {hasPendingInvitations && (
        <div className="success-message" style={{ marginBottom: '20px' }}>
          You have {user.pendingInvitationCount} pending invitation(s).{' '}
          <button
            className="btn btn-primary btn-small"
            onClick={() => navigate('/invitations')}
          >
            View Invitations
          </button>
        </div>
      )}

      {error && <div className="error-message">{error}</div>}

      <div className="org-section">
        <h2>Create Organisation</h2>
        <form onSubmit={handleCreate} style={{ marginTop: '15px' }}>
          <div className="form-group">
            <label>Organisation Name:</label>
            <input
              type="text"
              value={orgName}
              onChange={(e) => setOrgName(e.target.value)}
              placeholder="My Organisation"
              required
              minLength={3}
              maxLength={100}
            />
          </div>
          <button type="submit" className="btn btn-primary" disabled={loading} style={{ width: '100%' }}>
            {loading ? 'Creating...' : 'Create Organisation'}
          </button>
        </form>
      </div>

      <button
        onClick={logout}
        className="btn btn-secondary"
        style={{ marginTop: '20px', width: '100%' }}
      >
        Logout
      </button>
    </div>
  );
};

export default CreateOrgPage;
