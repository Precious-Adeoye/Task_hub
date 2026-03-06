import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { invitations, Invitation } from '../services';

const InvitationsPage: React.FC = () => {
  const { refreshUser } = useAuth();
  const navigate = useNavigate();
  const [pending, setPending] = useState<Invitation[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [message, setMessage] = useState('');

  useEffect(() => {
    loadInvitations();
  }, []);

  const loadInvitations = async () => {
    try {
      const response = await invitations.getMyPending();
      setPending(response.data);
    } catch (error) {
      console.error('Failed to load invitations', error);
    } finally {
      setLoading(false);
    }
  };

  const handleAccept = async (id: string) => {
    setActionLoading(id);
    setMessage('');
    try {
      await invitations.accept(id);
      setPending(prev => prev.filter(i => i.id !== id));
      await refreshUser();
      setMessage('Invitation accepted! You can now access the organisation.');
    } catch (error: any) {
      setMessage(error.response?.data?.detail || 'Failed to accept invitation');
    } finally {
      setActionLoading(null);
    }
  };

  const handleDecline = async (id: string) => {
    setActionLoading(id);
    setMessage('');
    try {
      await invitations.decline(id);
      setPending(prev => prev.filter(i => i.id !== id));
      setMessage('Invitation declined.');
    } catch (error: any) {
      setMessage(error.response?.data?.detail || 'Failed to decline invitation');
    } finally {
      setActionLoading(null);
    }
  };

  if (loading) {
    return <div className="container"><div className="loading">Loading invitations...</div></div>;
  }

  return (
    <div className="container" style={{ maxWidth: '600px' }}>
      <div className="header">
        <h1>Pending Invitations</h1>
        <button className="btn btn-secondary" onClick={() => navigate('/dashboard')}>
          Back to Dashboard
        </button>
      </div>

      {message && <div className="success-message">{message}</div>}

      {pending.length === 0 ? (
        <div className="empty-state">
          <p>No pending invitations.</p>
          <button className="btn btn-primary" style={{ marginTop: '15px' }} onClick={() => navigate('/dashboard')}>
            Go to Dashboard
          </button>
        </div>
      ) : (
        <div className="todo-list">
          {pending.map(inv => (
            <div key={inv.id} className="todo-item" style={{ flexDirection: 'column', alignItems: 'stretch' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                  <strong>{inv.organisationName}</strong>
                  <div style={{ fontSize: '13px', color: '#666' }}>
                    Invited by {inv.invitedByUsername} as <strong>{inv.role}</strong>
                  </div>
                  <div style={{ fontSize: '12px', color: '#999' }}>
                    {new Date(inv.createdAt).toLocaleDateString()}
                  </div>
                </div>
                <div className="todo-actions">
                  <button
                    className="btn btn-small btn-primary"
                    onClick={() => handleAccept(inv.id)}
                    disabled={actionLoading === inv.id}
                  >
                    Accept
                  </button>
                  <button
                    className="btn btn-small btn-danger"
                    onClick={() => handleDecline(inv.id)}
                    disabled={actionLoading === inv.id}
                  >
                    Decline
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default InvitationsPage;
