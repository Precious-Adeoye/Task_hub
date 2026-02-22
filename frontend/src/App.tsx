import React, { useState, useEffect } from 'react';
import { useAuth } from './contexts/AuthContext';
import { organisations, todos, Organisation, Todo } from './services';
import AuditLogViewer from './components/AuditLogViewer';
import ImportExport from './components/ImportExport';
import './App.css';

function App() {
  const { user, loading, login, register, logout } = useAuth();
  const [isLogin, setIsLogin] = useState(true);
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const [userOrgs, setUserOrgs] = useState<Organisation[]>([]);
  const [currentOrg, setCurrentOrg] = useState<Organisation | null>(null);
  const [newOrgName, setNewOrgName] = useState('');

  // Todo state
  const [todoList, setTodoList] = useState<Todo[]>([]);
  const [newTodoTitle, setNewTodoTitle] = useState('');
  const [newTodoDescription, setNewTodoDescription] = useState('');
  const [newTodoPriority, setNewTodoPriority] = useState<'Low' | 'Medium' | 'High'>('Medium');
  const [newTodoDueDate, setNewTodoDueDate] = useState('');
  const [newTodoTags, setNewTodoTags] = useState('');

  // Edit state
  const [editingTodo, setEditingTodo] = useState<Todo | null>(null);
  const [editTitle, setEditTitle] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [editPriority, setEditPriority] = useState<'Low' | 'Medium' | 'High'>('Medium');
  const [editDueDate, setEditDueDate] = useState('');
  const [editTags, setEditTags] = useState('');

  // Filter state
  const [filterStatus, setFilterStatus] = useState<'all' | 'Open' | 'Done'>('all');
  const [filterOverdue, setFilterOverdue] = useState(false);
  const [filterTag, setFilterTag] = useState('');
  const [showDeleted, setShowDeleted] = useState(false);
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortDesc, setSortDesc] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);

  // Loading states
  const [togglingIds, setTogglingIds] = useState<Set<string>>(new Set());
  const [errorMap, setErrorMap] = useState<Map<string, string>>(new Map());

  // Admin panel state
  const [showAuditLogs, setShowAuditLogs] = useState(false);
  const [showImportExport, setShowImportExport] = useState(false);
  const [isCurrentUserAdmin, setIsCurrentUserAdmin] = useState(false);

  // Add member state
  const [showAddMember, setShowAddMember] = useState(false);
  const [newMemberEmail, setNewMemberEmail] = useState('');
  const [newMemberRole, setNewMemberRole] = useState('Member');
  const [addMemberError, setAddMemberError] = useState('');
  const [addMemberSuccess, setAddMemberSuccess] = useState('');

  // Load organisations when user logs in
  useEffect(() => {
    if (user) {
      loadOrganisations();
    }
  }, [user]);

  // Check admin status when org or user changes
  useEffect(() => {
    if (currentOrg && user) {
      checkIfUserIsAdmin();
    }
  }, [currentOrg, user]);

  // Load todos when organisation or filters change
  useEffect(() => {
    if (currentOrg) {
      loadTodos();
    }
  }, [currentOrg, filterStatus, filterOverdue, filterTag, showDeleted, sortBy, sortDesc, page]);

  const checkIfUserIsAdmin = async () => {
    try {
      const members = await organisations.getMembers(currentOrg!.id);
      const currentMember = members.data.find((m: any) => m.userId === user?.id);
      setIsCurrentUserAdmin(currentMember?.role === 'OrgAdmin');
    } catch (error) {
      console.error('Failed to check admin status', error);
    }
  };

  const loadOrganisations = async () => {
    try {
      const response = await organisations.getMine();
      setUserOrgs(response.data);
      if (response.data.length > 0) {
        setCurrentOrg(response.data[0]);
        localStorage.setItem('currentOrganisationId', response.data[0].id);
      }
    } catch (error) {
      console.error('Failed to load organisations', error);
    }
  };

  const loadTodos = async () => {
    try {
      const params: any = {
        page,
        pageSize,
        sortBy,
        sortDescending: sortDesc,
        includeDeleted: showDeleted
      };

      if (filterStatus !== 'all') {
        params.status = filterStatus;
      }

      if (filterOverdue) {
        params.overdue = true;
      }

      if (filterTag) {
        params.tag = filterTag;
      }

      const response = await todos.getAll(params);
      setTodoList(response.data);
    } catch (error) {
      console.error('Failed to load todos', error);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      if (isLogin) {
        await login(username, password);
      } else {
        await register(username, email, password);
      }
    } catch (err: any) {
      setError(err.response?.data?.error || 'An error occurred');
    }
  };

  const handleCreateOrg = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newOrgName.trim()) return;
    try {
      const response = await organisations.create({ name: newOrgName });
      setUserOrgs([...userOrgs, response.data]);
      setNewOrgName('');
    } catch (error) {
      console.error('Failed to create organisation', error);
    }
  };

  const handleCreateTodo = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTodoTitle.trim() || !currentOrg) return;
    try {
      const data: any = {
        title: newTodoTitle,
        priority: newTodoPriority,
      };
      if (newTodoDescription.trim()) data.description = newTodoDescription;
      if (newTodoDueDate) data.dueDate = newTodoDueDate;
      if (newTodoTags.trim()) data.tags = newTodoTags.split(',').map(t => t.trim()).filter(Boolean);

      const response = await todos.create(data);
      setTodoList([...todoList, response.data]);
      setNewTodoTitle('');
      setNewTodoDescription('');
      setNewTodoPriority('Medium');
      setNewTodoDueDate('');
      setNewTodoTags('');
    } catch (error) {
      console.error('Failed to create todo', error);
    }
  };

  const handleToggleTodo = async (todo: Todo) => {
    setTogglingIds(prev => new Set(prev).add(todo.id));
    setErrorMap(prev => { const m = new Map(prev); m.delete(todo.id); return m; });
    try {
      const response = await todos.toggle(todo.id);
      setTodoList(todoList.map(t =>
        t.id === todo.id ? response.data : t
      ));
    } catch (error) {
      console.error('Failed to toggle todo', error);
      setErrorMap(prev => new Map(prev).set(todo.id, 'Failed to toggle'));
    } finally {
      setTogglingIds(prev => { const s = new Set(prev); s.delete(todo.id); return s; });
    }
  };

  const handleSoftDelete = async (id: string) => {
    try {
      await todos.softDelete(id);
      loadTodos();
    } catch (error) {
      console.error('Failed to delete todo', error);
    }
  };

  const handleRestore = async (id: string) => {
    try {
      await todos.restore(id);
      loadTodos();
    } catch (error) {
      console.error('Failed to restore todo', error);
    }
  };

  const handleHardDelete = async (id: string) => {
    if (!window.confirm('Permanently delete this todo? This cannot be undone.')) return;
    try {
      await todos.hardDelete(id);
      loadTodos();
    } catch (error) {
      console.error('Failed to permanently delete todo', error);
    }
  };

  const startEditing = (todo: Todo) => {
    setEditingTodo(todo);
    setEditTitle(todo.title);
    setEditDescription(todo.description || '');
    setEditPriority(todo.priority);
    setEditDueDate(todo.dueDate ? todo.dueDate.split('T')[0] : '');
    setEditTags(todo.tags.join(', '));
  };

  const cancelEditing = () => {
    setEditingTodo(null);
  };

  const handleUpdateTodo = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingTodo) return;
    try {
      const data: any = {
        title: editTitle,
        description: editDescription || null,
        priority: editPriority,
        tags: editTags ? editTags.split(',').map(t => t.trim()).filter(Boolean) : [],
        dueDate: editDueDate || null,
      };
      const response = await todos.update(editingTodo.id, data, editingTodo.version);
      setTodoList(todoList.map(t =>
        t.id === editingTodo.id ? response.data : t
      ));
      setEditingTodo(null);
    } catch (error: any) {
      if (error.response?.status === 412) {
        setErrorMap(prev => new Map(prev).set(editingTodo.id, 'Todo was modified by another user. Refresh and try again.'));
      } else {
        setErrorMap(prev => new Map(prev).set(editingTodo.id, 'Failed to update todo'));
      }
      console.error('Failed to update todo', error);
    }
  };

  const handleAddMember = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentOrg || !newMemberEmail.trim()) return;
    setAddMemberError('');
    setAddMemberSuccess('');
    try {
      await organisations.addMember(currentOrg.id, { email: newMemberEmail, role: newMemberRole });
      setAddMemberSuccess(`Member ${newMemberEmail} added successfully`);
      setNewMemberEmail('');
      setNewMemberRole('Member');
    } catch (error: any) {
      setAddMemberError(error.response?.data?.error || 'Failed to add member');
    }
  };

  if (loading) {
    return <div className="container">Loading...</div>;
  }

  if (!user) {
    return (
      <div className="container" style={{ maxWidth: '400px' }}>
        <h1>TaskHub</h1>
        <h2>{isLogin ? 'Login' : 'Register'}</h2>
        {error && <div style={{ color: 'red', marginBottom: '10px' }}>{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Username:</label>
            <input
              type="text"
              name="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
            />
          </div>
          {!isLogin && (
            <div className="form-group">
              <label>Email:</label>
              <input
                type="email"
                name="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
          )}
          <div className="form-group">
            <label>Password:</label>
            <input
              type="password"
              name="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>
          <button type="submit" className="btn btn-primary" style={{ width: '100%' }}>
            {isLogin ? 'Login' : 'Register'}
          </button>
        </form>
        <button
          onClick={() => setIsLogin(!isLogin)}
          className="btn btn-secondary"
          style={{ marginTop: '10px', width: '100%' }}
        >
          Switch to {isLogin ? 'Register' : 'Login'}
        </button>
      </div>
    );
  }

  return (
    <div className="container">
      {/* Header */}
      <div className="header">
        <h1>TaskHub</h1>
        <div>
          Welcome, {user.username}!
          <button onClick={logout} className="btn btn-secondary btn-small" style={{ marginLeft: '10px' }}>
            Logout
          </button>
        </div>
      </div>

      {/* Organisation Section */}
      <div className="org-section">
        <h2>Organisation: {currentOrg?.name}</h2>
        <div className="org-controls">
          <select
            className="org-select"
            value={currentOrg?.id || ''}
            onChange={(e) => {
              const org = userOrgs.find(o => o.id === e.target.value);
              setCurrentOrg(org || null);
              if (org) localStorage.setItem('currentOrganisationId', org.id);
            }}
          >
            {userOrgs.map(org => (
              <option key={org.id} value={org.id}>{org.name}</option>
            ))}
          </select>

          <form onSubmit={handleCreateOrg} className="create-org-form">
            <input
              type="text"
              value={newOrgName}
              onChange={(e) => setNewOrgName(e.target.value)}
              placeholder="New organisation name"
            />
            <button type="submit" className="btn btn-primary">Create</button>
          </form>
        </div>
      </div>

      {/* Todos */}
      {currentOrg && (
        <div className="todos-section">
          <h2>Todos</h2>

          {/* Create Todo Form */}
          <div className="create-todo-form">
            <form onSubmit={handleCreateTodo}>
              <div className="form-row">
                <div className="form-group">
                  <label>Title</label>
                  <input
                    type="text"
                    value={newTodoTitle}
                    onChange={(e) => setNewTodoTitle(e.target.value)}
                    placeholder="What needs to be done?"
                    required
                  />
                </div>
                <div className="form-group">
                  <label>Priority</label>
                  <select
                    value={newTodoPriority}
                    onChange={(e) => setNewTodoPriority(e.target.value as 'Low' | 'Medium' | 'High')}
                  >
                    <option value="Low">Low</option>
                    <option value="Medium">Medium</option>
                    <option value="High">High</option>
                  </select>
                </div>
                <div className="form-group">
                  <label>Due Date</label>
                  <input
                    type="date"
                    value={newTodoDueDate}
                    onChange={(e) => setNewTodoDueDate(e.target.value)}
                  />
                </div>
              </div>
              <div className="form-group">
                <label>Description</label>
                <textarea
                  value={newTodoDescription}
                  onChange={(e) => setNewTodoDescription(e.target.value)}
                  placeholder="Optional description..."
                  rows={2}
                />
              </div>
              <div className="form-group">
                <label>Tags (comma-separated)</label>
                <input
                  type="text"
                  value={newTodoTags}
                  onChange={(e) => setNewTodoTags(e.target.value)}
                  placeholder="e.g. urgent, frontend, bug"
                />
              </div>
              <button type="submit" className="btn btn-primary">Add Todo</button>
            </form>
          </div>

          {/* Filters */}
          <div className="filters-section">
            <h3>Filters</h3>
            <div className="filter-controls">
              <div className="filter-group">
                <label>Status:</label>
                <select value={filterStatus} onChange={(e) => { setFilterStatus(e.target.value as any); setPage(1); }}>
                  <option value="all">All</option>
                  <option value="Open">Open</option>
                  <option value="Done">Done</option>
                </select>
              </div>
              <div className="filter-group">
                <label>Sort:</label>
                <select value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
                  <option value="createdAt">Created</option>
                  <option value="dueDate">Due Date</option>
                  <option value="priority">Priority</option>
                  <option value="title">Title</option>
                </select>
                <button className="btn btn-small btn-secondary" onClick={() => setSortDesc(!sortDesc)}>
                  {sortDesc ? 'Desc' : 'Asc'}
                </button>
              </div>
              <div className="filter-group">
                <label>Tag:</label>
                <input
                  type="text"
                  value={filterTag}
                  onChange={(e) => { setFilterTag(e.target.value); setPage(1); }}
                  placeholder="Filter by tag"
                />
              </div>
              <div className="filter-group">
                <label>
                  <input type="checkbox" checked={filterOverdue} onChange={(e) => { setFilterOverdue(e.target.checked); setPage(1); }} />
                  {' '}Overdue only
                </label>
              </div>
              <div className="filter-group">
                <label>
                  <input type="checkbox" checked={showDeleted} onChange={(e) => { setShowDeleted(e.target.checked); setPage(1); }} />
                  {' '}Show deleted
                </label>
              </div>
            </div>
          </div>

          {/* Todo List */}
          <div className="todo-list">
            {todoList.length === 0 && (
              <div className="empty-state">No todos found.</div>
            )}
            {todoList.map(todo => {
              const isDeleted = !!todo.deletedAt;
              const isEditing = editingTodo?.id === todo.id;

              if (isEditing) {
                return (
                  <div key={todo.id} className="todo-item editing">
                    <form onSubmit={handleUpdateTodo} className="edit-form">
                      <div className="form-row">
                        <div className="form-group">
                          <label>Title</label>
                          <input
                            type="text"
                            value={editTitle}
                            onChange={(e) => setEditTitle(e.target.value)}
                            required
                          />
                        </div>
                        <div className="form-group">
                          <label>Priority</label>
                          <select
                            value={editPriority}
                            onChange={(e) => setEditPriority(e.target.value as 'Low' | 'Medium' | 'High')}
                          >
                            <option value="Low">Low</option>
                            <option value="Medium">Medium</option>
                            <option value="High">High</option>
                          </select>
                        </div>
                        <div className="form-group">
                          <label>Due Date</label>
                          <input
                            type="date"
                            value={editDueDate}
                            onChange={(e) => setEditDueDate(e.target.value)}
                          />
                        </div>
                      </div>
                      <div className="form-group">
                        <label>Description</label>
                        <textarea
                          value={editDescription}
                          onChange={(e) => setEditDescription(e.target.value)}
                          rows={2}
                        />
                      </div>
                      <div className="form-group">
                        <label>Tags (comma-separated)</label>
                        <input
                          type="text"
                          value={editTags}
                          onChange={(e) => setEditTags(e.target.value)}
                        />
                      </div>
                      <div className="edit-actions">
                        <button type="submit" className="btn btn-small btn-primary">Save</button>
                        <button type="button" className="btn btn-small btn-secondary" onClick={cancelEditing}>Cancel</button>
                      </div>
                    </form>
                  </div>
                );
              }

              return (
                <div
                  key={todo.id}
                  className={`todo-item${isDeleted ? ' deleted' : ''}`}
                >
                  <input
                    type="checkbox"
                    checked={todo.status === 'Done'}
                    onChange={() => handleToggleTodo(todo)}
                    disabled={togglingIds.has(todo.id) || isDeleted}
                  />
                  <div style={{ flex: 1 }}>
                    <span style={{
                      textDecoration: todo.status === 'Done' ? 'line-through' : 'none',
                      fontWeight: 500,
                    }}>
                      {todo.title}
                    </span>
                    {todo.description && (
                      <div style={{ fontSize: '12px', color: '#777', marginTop: '2px' }}>
                        {todo.description}
                      </div>
                    )}
                    {todo.tags && todo.tags.length > 0 && (
                      <div style={{ marginTop: '4px' }}>
                        {todo.tags.map(tag => (
                          <span key={tag} style={{
                            display: 'inline-block',
                            padding: '1px 6px',
                            marginRight: '4px',
                            background: '#e0e0e0',
                            borderRadius: '3px',
                            fontSize: '11px',
                          }}>
                            {tag}
                          </span>
                        ))}
                      </div>
                    )}
                    {errorMap.has(todo.id) && (
                      <div style={{ color: 'red', fontSize: '12px' }}>{errorMap.get(todo.id)}</div>
                    )}
                  </div>
                  {todo.dueDate && (
                    <span style={{ fontSize: '12px', color: '#999' }}>
                      Due: {new Date(todo.dueDate).toLocaleDateString()}
                    </span>
                  )}
                  <span style={{
                    padding: '2px 8px',
                    borderRadius: '4px',
                    backgroundColor:
                      todo.priority === 'High' ? '#e74c3c' :
                      todo.priority === 'Medium' ? '#f39c12' : '#27ae60',
                    color: 'white',
                    fontSize: '12px',
                  }}>
                    {todo.priority}
                  </span>
                  {isDeleted ? (
                    <div className="todo-actions">
                      <button
                        className="btn btn-small btn-warning"
                        title="Restore"
                        onClick={() => handleRestore(todo.id)}
                      >
                        Restore
                      </button>
                      {isCurrentUserAdmin && (
                        <button
                          className="btn btn-small btn-danger"
                          title="Permanently delete"
                          onClick={() => handleHardDelete(todo.id)}
                        >
                          Delete Forever
                        </button>
                      )}
                    </div>
                  ) : (
                    <div className="todo-actions">
                      <button
                        className="btn btn-small btn-secondary"
                        title="Edit"
                        onClick={() => startEditing(todo)}
                      >
                        Edit
                      </button>
                      <button
                        className="btn btn-small btn-danger"
                        title="Move to trash"
                        onClick={() => handleSoftDelete(todo.id)}
                      >
                        Delete
                      </button>
                    </div>
                  )}
                </div>
              );
            })}
          </div>

          {/* Pagination */}
          <div style={{ display: 'flex', gap: '10px', justifyContent: 'center', marginTop: '20px' }}>
            <button className="btn btn-small btn-secondary" disabled={page <= 1} onClick={() => setPage(page - 1)}>
              Previous
            </button>
            <span style={{ padding: '5px 10px' }}>Page {page}</span>
            <button className="btn btn-small btn-secondary" disabled={todoList.length < pageSize} onClick={() => setPage(page + 1)}>
              Next
            </button>
          </div>
        </div>
      )}

      {/* Admin Tools */}
      {currentOrg && isCurrentUserAdmin && (
        <section className="admin-section">
          <h2>Admin Tools</h2>

          <div className="admin-tabs">
            <button
              className={`admin-tab ${showAuditLogs ? 'active' : ''}`}
              onClick={() => {
                setShowAuditLogs(true);
                setShowImportExport(false);
                setShowAddMember(false);
              }}
            >
              Audit Logs
            </button>
            <button
              className={`admin-tab ${showImportExport ? 'active' : ''}`}
              onClick={() => {
                setShowAuditLogs(false);
                setShowImportExport(true);
                setShowAddMember(false);
              }}
            >
              Import/Export
            </button>
            <button
              className={`admin-tab ${showAddMember ? 'active' : ''}`}
              onClick={() => {
                setShowAuditLogs(false);
                setShowImportExport(false);
                setShowAddMember(true);
              }}
            >
              Add Member
            </button>
          </div>

          {showAuditLogs && (
            <AuditLogViewer organisationId={currentOrg.id} />
          )}

          {showImportExport && (
            <ImportExport organisationId={currentOrg.id} />
          )}

          {showAddMember && (
            <div className="add-member-section">
              <h3>Add Member to Organisation</h3>
              {addMemberError && <div className="error-message">{addMemberError}</div>}
              {addMemberSuccess && <div className="success-message">{addMemberSuccess}</div>}
              <form onSubmit={handleAddMember} className="add-member-form">
                <div className="form-group">
                  <label>Email:</label>
                  <input
                    type="email"
                    value={newMemberEmail}
                    onChange={(e) => setNewMemberEmail(e.target.value)}
                    placeholder="member@example.com"
                    required
                  />
                </div>
                <div className="form-group">
                  <label>Role:</label>
                  <select
                    value={newMemberRole}
                    onChange={(e) => setNewMemberRole(e.target.value)}
                  >
                    <option value="Member">Member</option>
                    <option value="Admin">Admin</option>
                  </select>
                </div>
                <button type="submit" className="btn btn-primary">Add</button>
              </form>
            </div>
          )}
        </section>
      )}
    </div>
  );
}

export default App;
