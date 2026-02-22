import React, { useState, useEffect } from 'react';
import { audit, AuditEntry } from '../services';

interface AuditLogViewerProps {
  organisationId: string;
}

const AuditLogViewer: React.FC<AuditLogViewerProps> = ({ organisationId }) => {
  const [logs, setLogs] = useState<AuditEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [fromDate, setFromDate] = useState(() => {
    const date = new Date();
    date.setDate(date.getDate() - 7);
    return date.toISOString().split('T')[0];
  });
  const [toDate, setToDate] = useState(() => {
    return new Date().toISOString().split('T')[0];
  });
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 20;

  useEffect(() => {
    loadAuditLogs();
  }, [organisationId, fromDate, toDate, page]);

  const loadAuditLogs = async () => {
    setLoading(true);
    try {
      const response = await audit.getLogs({
        from: fromDate,
        to: toDate,
        page,
        pageSize,
      });
      setLogs(response.data.logs);
      setTotalCount(response.data.totalCount);
    } catch (err) {
      setError('Failed to load audit logs');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const formatActionType = (action: string) => {
    return action.replace(/([A-Z])/g, ' $1').trim();
  };

  const getActionColor = (action: string) => {
    if (action.includes('Login')) return '#3498db';
    if (action.includes('Created')) return '#27ae60';
    if (action.includes('Updated')) return '#f39c12';
    if (action.includes('Deleted')) return '#e74c3c';
    return '#95a5a6';
  };

  if (loading && logs.length === 0) {
    return <div className="loading">Loading audit logs...</div>;
  }

  return (
    <div className="audit-log-viewer">
      <h2>Audit Logs</h2>

      <div className="audit-filters">
        <div className="filter-group">
          <label htmlFor="from-date">From:</label>
          <input
            id="from-date"
            type="date"
            value={fromDate}
            onChange={(e) => {
              setFromDate(e.target.value);
              setPage(1);
            }}
          />
        </div>

        <div className="filter-group">
          <label htmlFor="to-date">To:</label>
          <input
            id="to-date"
            type="date"
            value={toDate}
            onChange={(e) => {
              setToDate(e.target.value);
              setPage(1);
            }}
          />
        </div>

        <button
          onClick={() => {
            setPage(1);
            loadAuditLogs();
          }}
          className="btn btn-primary"
        >
          Apply Filters
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="audit-table-container">
        <table className="audit-table">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>Action</th>
              <th>Entity</th>
              <th>Details</th>
              <th>Correlation ID</th>
            </tr>
          </thead>
          <tbody>
            {logs.map(log => (
              <tr key={log.id}>
                <td>{new Date(log.timestamp).toLocaleString()}</td>
                <td>
                  <span
                    className="action-badge"
                    style={{ backgroundColor: getActionColor(log.actionType) }}
                  >
                    {formatActionType(log.actionType)}
                  </span>
                </td>
                <td>
                  <div className="entity-info">
                    <strong>{log.entityType}</strong>
                    <small>{log.entityId}</small>
                  </div>
                </td>
                <td>{log.details}</td>
                <td>
                  <code className="correlation-id">{log.correlationId}</code>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="pagination">
        <button
          onClick={() => setPage(p => Math.max(1, p - 1))}
          disabled={page === 1}
          className="btn btn-small btn-secondary"
        >
          Previous
        </button>
        <span>
          Page {page} of {Math.ceil(totalCount / pageSize) || 1}
        </span>
        <button
          onClick={() => setPage(p => p + 1)}
          disabled={page >= Math.ceil(totalCount / pageSize)}
          className="btn btn-small btn-secondary"
        >
          Next
        </button>
      </div>
    </div>
  );
};

export default AuditLogViewer;
