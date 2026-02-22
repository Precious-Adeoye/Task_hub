import React, { useState, useRef } from 'react';
import { importExport, ImportResult } from '../services';

interface ImportExportProps {
  organisationId: string;
}

const ImportExport: React.FC<ImportExportProps> = ({ organisationId }) => {
  const [activeTab, setActiveTab] = useState<'export' | 'import'>('export');
  const [format, setFormat] = useState<'json' | 'csv'>('json');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<ImportResult | null>(null);
  const [error, setError] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleExport = async () => {
    setLoading(true);
    setError('');

    try {
      const response = await importExport.exportTodos(format);

      // Create download link
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `todos-export.${format}`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      setError('Failed to export todos');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleImport = async (file: File) => {
    setLoading(true);
    setError('');
    setResult(null);

    try {
      const response = await importExport.importTodos(file, format);
      setResult(response.data);

      // Clear file input
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } catch (err: any) {
      setError(err.response?.data?.detail || 'Failed to import todos');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      handleImport(file);
    }
  };

  const downloadTemplate = async () => {
    try {
      const response = await importExport.getTemplate(format);

      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `import-template.${format}`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      setError('Failed to download template');
      console.error(err);
    }
  };

  return (
    <div className="import-export">
      <h2>Import/Export Todos</h2>

      <div className="tab-bar">
        <button
          className={`tab ${activeTab === 'export' ? 'active' : ''}`}
          onClick={() => setActiveTab('export')}
        >
          Export
        </button>
        <button
          className={`tab ${activeTab === 'import' ? 'active' : ''}`}
          onClick={() => setActiveTab('import')}
        >
          Import
        </button>
      </div>

      <div className="format-selector">
        <label>Format:</label>
        <select value={format} onChange={(e) => setFormat(e.target.value as 'json' | 'csv')}>
          <option value="json">JSON</option>
          <option value="csv">CSV</option>
        </select>
      </div>

      {activeTab === 'export' ? (
        <div className="export-section">
          <p>Export all todos for this organisation.</p>
          <button
            onClick={handleExport}
            disabled={loading}
            className="btn btn-primary"
          >
            {loading ? 'Exporting...' : `Export as ${format.toUpperCase()}`}
          </button>
        </div>
      ) : (
        <div className="import-section">
          <p>Import todos from a file. The import is idempotent - duplicate entries will be skipped.</p>

          <div className="import-controls">
            <button
              onClick={downloadTemplate}
              className="btn btn-secondary"
              disabled={loading}
            >
              Download Template
            </button>

            <input
              type="file"
              ref={fileInputRef}
              onChange={handleFileChange}
              accept={format === 'json' ? '.json' : '.csv'}
              disabled={loading}
              style={{ display: 'none' }}
            />

            <button
              onClick={() => fileInputRef.current?.click()}
              className="btn btn-primary"
              disabled={loading}
            >
              {loading ? 'Importing...' : 'Select File to Import'}
            </button>
          </div>

          {error && <div className="error-message">{error}</div>}

          {result && (
            <div className="import-result">
              <h3>Import Result</h3>
              <div className="result-stats">
                <div className="stat accepted">
                  <span className="stat-value">{result.acceptedCount}</span>
                  <span className="stat-label">Accepted</span>
                </div>
                <div className="stat rejected">
                  <span className="stat-value">{result.rejectedCount}</span>
                  <span className="stat-label">Rejected</span>
                </div>
              </div>

              {result.errors.length > 0 && (
                <div className="import-errors">
                  <h4>Rejected Rows</h4>
                  <table className="error-table">
                    <thead>
                      <tr>
                        <th>Row</th>
                        <th>ID</th>
                        <th>Error</th>
                      </tr>
                    </thead>
                    <tbody>
                      {result.errors.map((err, index) => (
                        <tr key={index}>
                          <td>{err.rowNumber}</td>
                          <td>{err.clientProvidedId || '-'}</td>
                          <td className="error-message">{err.errorMessage}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default ImportExport;
