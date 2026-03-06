import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { organisations, todos, invitations, Organisation, Todo, Member, Invitation } from '../services';
import AuditLogViewer from '../components/AuditLogViewer';
import ImportExport from '../components/ImportExport';

const DashboardPage: React.FC = () => {
  const { user, logout, refreshUser } = useAuth();
  const navigate = useNavigate();

  // Org state
  const [userOrgs, setUserOrgs] = useState<Organisation[]>([]);
  const [currentOrg, setCurrentOrg] = useState<Organisation | null>(null);
  const [newOrgName, setNewOrgName] = useState('');

  // Members state
  const [members, setMembers] = useState<Member[]>([]);
  const [isCurrentUserAdmin, setIsCurrentUserAdmin] = useState(false);

  // Todo state
  const [todoList, setTodoList] = useState<Todo[]>([]);
  const [newTodoTitle, setNewTodoTitle] = useState('');
  const [newTodoDescription, setNewTodoDescription] = useState('');
  const [newTodoPriority, setNewTodoPriority] = useState<'Low' | 'Medium' | 'High'>('Medium');
  const [newTodoDueDate, setNewTodoDueDate] = useState('');
  const [newTodoTags, setNewTodoTags] = useState('');
  const [newTodoAssignee, setNewTodoAssignee] = useState('');
  const [createTodoError, setCreateTodoError] = useState('');

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

  // UI state
  const [errorMap, setErrorMap] = useState<Map<string, string>>(new Map());
  const [activeAdminTab, setActiveAdminTab] = useState<string | null>(null);

  // Invitation state
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('Member');
  const [inviteError, setInviteError] = useState('');
  const [inviteSuccess, setInviteSuccess] = useState('');
  const [orgInvitations, setOrgInvitations] = useState<Invitation[]>([]);

  const loadOrganisations = useCallback(async () => {
    try {
      const response = await organisations.getMine();
      setUserOrgs(response.data);
      const savedOrgId = localStorage.getItem('currentOrganisationId');
      const savedOrg = response.data.find((o: Organisation) => o.id === savedOrgId);
      if (savedOrg) {
        setCurrentOrg(savedOrg);
      } else if (response.data.length > 0) {
        setCurrentOrg(response.data[0]);
        localStorage.setItem('currentOrganisationId', response.data[0].id);
      }
    } catch (error) {
      console.error('Failed to load organisations', error);
    }
  }, []);

  const loadMembers = useCallback(async () => {
    if (!currentOrg) return;
    try {
      const response = await organisations.getMembers(currentOrg.id);
      setMembers(response.data);
      const currentMember = response.data.find((m: Member) => m.userId === user?.id);
      setIsCurrentUserAdmin(currentMember?.role === 'OrgAdmin');
    } catch {
      setIsCurrentUserAdmin(false);
    }
  }, [currentOrg, user?.id]);

  const loadTodos = useCallback(async () => {
    if (!currentOrg) return;
    try {
      const params: any = { page, pageSize, sortBy, sortDescending: sortDesc, includeDeleted: showDeleted };
      if (filterStatus !== 'all') params.status = filterStatus;
      if (filterOverdue) params.overdue = true;
      if (filterTag) params.tag = filterTag;

      const response = await todos.getAll(params);
      setTodoList(response.data);
    } catch (error) {
      console.error('Failed to load todos', error);
    }
  }, [currentOrg, filterStatus, filterOverdue, filterTag, showDeleted, sortBy, sortDesc, page, pageSize]);

  useEffect(() => {
    if (user) loadOrganisations();
  }, [user, loadOrganisations]);

  useEffect(() => {
    if (currentOrg) {
      loadMembers();
      loadTodos();
    }
  }, [currentOrg, loadMembers, loadTodos]);

  const handleCreateOrg = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newOrgName.trim()) return;
    try {
      const response = await organisations.create({ name: newOrgName });
      setUserOrgs([...userOrgs, response.data]);
      setCurrentOrg(response.data);
      localStorage.setItem('currentOrganisationId', response.data.id);
      setNewOrgName('');
      await refreshUser();
    } catch (error) {
      console.error('Failed to create organisation', error);
    }
  };

  const handleCreateTodo = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTodoTitle.trim() || !currentOrg) return;
    setCreateTodoError('');
    try {
      const data: any = { title: newTodoTitle, priority: newTodoPriority };
      if (newTodoDescription.trim()) data.description = newTodoDescription;
      if (newTodoDueDate) data.dueDate = newTodoDueDate;
      if (newTodoTags.trim()) data.tags = newTodoTags.split(',').map((t: string) => t.trim().replace(/\s+/g, '-')).filter(Boolean);
      if (newTodoAssignee) data.assignedTo = newTodoAssignee;

      const response = await todos.create(data);
      setTodoList([response.data, ...todoList]);
      setNewTodoTitle('');
      setNewTodoDescription('');
      setNewTodoPriority('Medium');
      setNewTodoDueDate('');
      setNewTodoTags('');
      setNewTodoAssignee('');
    } catch (error: any) {
      const errors = error.response?.data?.errors;
      if (errors) {
        setCreateTodoError(Object.values(errors).flat().join('. '));
      } else {
        setCreateTodoError('Failed to create todo');
      }
    }
  };

  const handleToggleTodo = async (todo: Todo) => {
    setErrorMap(prev => { const m = new Map(prev); m.delete(todo.id); return m; });
    const previousList = [...todoList];
    setTodoList(todoList.map(t => t.id === todo.id ? { ...t, status: t.status === 'Done' ? 'Open' : 'Done' } : t));

    try {
      const response = await todos.toggle(todo.id);
      setTodoList(prev => prev.map(t => t.id === todo.id ? response.data : t));
    } catch {
      setTodoList(previousList);
      setErrorMap(prev => new Map(prev).set(todo.id, 'Failed to toggle. Change has been reverted.'));
    }
  };

  const handleSoftDelete = async (id: string) => {
    try { await todos.softDelete(id); loadTodos(); } catch { console.error('Failed to delete'); }
  };

  const handleRestore = async (id: string) => {
    try { await todos.restore(id); loadTodos(); } catch { console.error('Failed to restore'); }
  };

  const handleHardDelete = async (id: string) => {
    if (!window.confirm('Permanently delete this todo? This cannot be undone.')) return;
    try { await todos.hardDelete(id); loadTodos(); } catch { console.error('Failed to permanently delete'); }
  };

  const handleAssign = async (todoId: string, assignedTo: string | null) => {
    try {
      const response = await todos.assign(todoId, assignedTo);
      setTodoList(prev => prev.map(t => t.id === todoId ? response.data : t));
    } catch {
      setErrorMap(prev => new Map(prev).set(todoId, 'Failed to assign'));
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

  const handleUpdateTodo = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingTodo) return;
    try {
      const data: any = {
        title: editTitle,
        description: editDescription || null,
        priority: editPriority,
        tags: editTags ? editTags.split(',').map((t: string) => t.trim().replace(/\s+/g, '-')).filter(Boolean) : [],
        dueDate: editDueDate || null,
      };
      const response = await todos.update(editingTodo.id, data, editingTodo.version);
      setTodoList(todoList.map(t => t.id === editingTodo.id ? response.data : t));
      setEditingTodo(null);
    } catch (error: any) {
      if (error.response?.status === 412) {
        setErrorMap(prev => new Map(prev).set(editingTodo.id, 'Todo was modified by another user. Refresh and try again.'));
      } else {
        setErrorMap(prev => new Map(prev).set(editingTodo.id, 'Failed to update todo'));
      }
    }
  };

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentOrg || !inviteEmail.trim()) return;
    setInviteError('');
    setInviteSuccess('');
    try {
      await invitations.create(currentOrg.id, { email: inviteEmail, role: inviteRole });
      setInviteSuccess(`Invitation sent to ${inviteEmail}`);
      setInviteEmail('');
      setInviteRole('Member');
      loadOrgInvitations();
    } catch (error: any) {
      setInviteError(error.response?.data?.detail || 'Failed to send invitation');
    }
  };

  const loadOrgInvitations = async () => {
    if (!currentOrg) return;
    try {
      const response = await invitations.getForOrg(currentOrg.id);
      setOrgInvitations(response.data);
    } catch { /* admin only */ }
  };

  useEffect(() => {
    if (activeAdminTab === 'invitations' && currentOrg) {
      loadOrgInvitations();
    }
  }, [activeAdminTab, currentOrg]);

  if (!user) return null;

  return (
    <div className="container">
      {/* Header */}
      <div className="header">
        <h1>TaskHub</h1>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <span>Welcome, {user.username}!</span>
          {user.pendingInvitationCount > 0 && (
            <button className="btn btn-warning btn-small" onClick={() => navigate('/invitations')}>
              {user.pendingInvitationCount} Invitation(s)
            </button>
          )}
          <button onClick={logout} className="btn btn-secondary btn-small">Logout</button>
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
          {isCurrentUserAdmin && (
            <div className="create-todo-form">
              {createTodoError && <div className="error-message">{createTodoError}</div>}
              <form onSubmit={handleCreateTodo}>
                <div className="form-row">
                  <div className="form-group">
                    <label>Title</label>
                    <input type="text" value={newTodoTitle} onChange={(e) => setNewTodoTitle(e.target.value)} placeholder="What needs to be done?" required />
                  </div>
                  <div className="form-group">
                    <label>Priority</label>
                    <select value={newTodoPriority} onChange={(e) => setNewTodoPriority(e.target.value as any)}>
                      <option value="Low">Low</option>
                      <option value="Medium">Medium</option>
                      <option value="High">High</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label>Due Date</label>
                    <input type="date" value={newTodoDueDate} onChange={(e) => setNewTodoDueDate(e.target.value)} min={new Date().toISOString().split('T')[0]} />
                  </div>
                </div>
                <div className="form-group">
                  <label>Description</label>
                  <textarea value={newTodoDescription} onChange={(e) => setNewTodoDescription(e.target.value)} placeholder="Optional description..." rows={2} />
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '15px' }}>
                  <div className="form-group">
                    <label>Tags (comma-separated)</label>
                    <input type="text" value={newTodoTags} onChange={(e) => setNewTodoTags(e.target.value)} placeholder="e.g. urgent, frontend" />
                  </div>
                  <div className="form-group">
                    <label>Assign to</label>
                    <select value={newTodoAssignee} onChange={(e) => setNewTodoAssignee(e.target.value)}>
                      <option value="">Unassigned</option>
                      {members.map(m => (
                        <option key={m.userId} value={m.userId}>{m.username}</option>
                      ))}
                    </select>
                  </div>
                </div>
                <button type="submit" className="btn btn-primary">Add Todo</button>
              </form>
            </div>
          )}

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
                </select>
                <button className="btn btn-small btn-secondary" onClick={() => setSortDesc(!sortDesc)}>
                  {sortDesc ? 'Desc' : 'Asc'}
                </button>
              </div>
              <div className="filter-group">
                <label>Tag:</label>
                <input type="text" value={filterTag} onChange={(e) => { setFilterTag(e.target.value); setPage(1); }} placeholder="Filter by tag" />
              </div>
              <div className="filter-group">
                <label><input type="checkbox" checked={filterOverdue} onChange={(e) => { setFilterOverdue(e.target.checked); setPage(1); }} /> Overdue only</label>
              </div>
              <div className="filter-group">
                <label><input type="checkbox" checked={showDeleted} onChange={(e) => { setShowDeleted(e.target.checked); setPage(1); }} /> Show deleted</label>
              </div>
            </div>
          </div>

          {/* Todo List */}
          <div className="todo-list">
            {todoList.length === 0 && <div className="empty-state">No todos found.</div>}
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
                          <input type="text" value={editTitle} onChange={(e) => setEditTitle(e.target.value)} required />
                        </div>
                        <div className="form-group">
                          <label>Priority</label>
                          <select value={editPriority} onChange={(e) => setEditPriority(e.target.value as any)}>
                            <option value="Low">Low</option>
                            <option value="Medium">Medium</option>
                            <option value="High">High</option>
                          </select>
                        </div>
                        <div className="form-group">
                          <label>Due Date</label>
                          <input type="date" value={editDueDate} onChange={(e) => setEditDueDate(e.target.value)} />
                        </div>
                      </div>
                      <div className="form-group">
                        <label>Description</label>
                        <textarea value={editDescription} onChange={(e) => setEditDescription(e.target.value)} rows={2} />
                      </div>
                      <div className="form-group">
                        <label>Tags (comma-separated)</label>
                        <input type="text" value={editTags} onChange={(e) => setEditTags(e.target.value)} />
                      </div>
                      <div className="edit-actions">
                        <button type="submit" className="btn btn-small btn-primary">Save</button>
                        <button type="button" className="btn btn-small btn-secondary" onClick={() => setEditingTodo(null)}>Cancel</button>
                      </div>
                    </form>
                  </div>
                );
              }

              // Members can only toggle their own assigned todos
              const canToggle = isCurrentUserAdmin || todo.assignedTo === user?.id;
              const canEdit = isCurrentUserAdmin;

              return (
                <div key={todo.id} className={`todo-item${isDeleted ? ' deleted' : ''}`}>
                  <input
                    type="checkbox"
                    checked={todo.status === 'Done'}
                    onChange={() => handleToggleTodo(todo)}
                    disabled={isDeleted || !canToggle}
                  />
                  <div style={{ flex: 1 }}>
                    <span style={{ textDecoration: todo.status === 'Done' ? 'line-through' : 'none', fontWeight: 500 }}>
                      {todo.title}
                    </span>
                    {todo.description && (
                      <div style={{ fontSize: '12px', color: '#777', marginTop: '2px' }}>{todo.description}</div>
                    )}
                    <div style={{ display: 'flex', gap: '8px', marginTop: '4px', flexWrap: 'wrap', alignItems: 'center' }}>
                      {todo.assignedToUsername && (
                        <span style={{ fontSize: '12px', color: '#3498db', background: '#ebf5fb', padding: '1px 6px', borderRadius: '3px' }}>
                          @{todo.assignedToUsername}
                        </span>
                      )}
                      {todo.tags && todo.tags.length > 0 && todo.tags.map(tag => (
                        <span key={tag} style={{ display: 'inline-block', padding: '1px 6px', background: '#e0e0e0', borderRadius: '3px', fontSize: '11px' }}>
                          {tag}
                        </span>
                      ))}
                    </div>
                    {errorMap.has(todo.id) && (
                      <div style={{ color: 'red', fontSize: '12px' }}>{errorMap.get(todo.id)}</div>
                    )}
                  </div>
                  {todo.dueDate && (
                    <span style={{ fontSize: '12px', color: '#999' }}>Due: {new Date(todo.dueDate).toLocaleDateString()}</span>
                  )}
                  <span style={{
                    padding: '2px 8px', borderRadius: '4px', color: 'white', fontSize: '12px',
                    backgroundColor: todo.priority === 'High' ? '#e74c3c' : todo.priority === 'Medium' ? '#f39c12' : '#27ae60',
                  }}>
                    {todo.priority}
                  </span>

                  {/* Assign dropdown (admin only) */}
                  {isCurrentUserAdmin && !isDeleted && (
                    <select
                      value={todo.assignedTo || ''}
                      onChange={(e) => handleAssign(todo.id, e.target.value || null)}
                      style={{ padding: '4px', fontSize: '12px', borderRadius: '4px', border: '1px solid #ddd' }}
                    >
                      <option value="">Unassigned</option>
                      {members.map(m => (
                        <option key={m.userId} value={m.userId}>{m.username}</option>
                      ))}
                    </select>
                  )}

                  {isDeleted ? (
                    <div className="todo-actions">
                      <button className="btn btn-small btn-warning" onClick={() => handleRestore(todo.id)}>Restore</button>
                      {isCurrentUserAdmin && (
                        <button className="btn btn-small btn-danger" onClick={() => handleHardDelete(todo.id)}>Delete Forever</button>
                      )}
                    </div>
                  ) : (
                    <div className="todo-actions">
                      {canEdit && <button className="btn btn-small btn-secondary" onClick={() => startEditing(todo)}>Edit</button>}
                      {(canEdit || todo.assignedTo === user?.id) && (
                        <button className="btn btn-small btn-danger" onClick={() => handleSoftDelete(todo.id)}>Delete</button>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>

          {/* Pagination */}
          <div style={{ display: 'flex', gap: '10px', justifyContent: 'center', marginTop: '20px' }}>
            <button className="btn btn-small btn-secondary" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button>
            <span style={{ padding: '5px 10px' }}>Page {page}</span>
            <button className="btn btn-small btn-secondary" disabled={todoList.length < pageSize} onClick={() => setPage(page + 1)}>Next</button>
          </div>
        </div>
      )}

      {/* Admin Tools */}
      {currentOrg && isCurrentUserAdmin && (
        <section className="admin-section">
          <h2>Admin Tools</h2>
          <div className="admin-tabs">
            <button className={`admin-tab ${activeAdminTab === 'invitations' ? 'active' : ''}`} onClick={() => setActiveAdminTab(activeAdminTab === 'invitations' ? null : 'invitations')}>
              Invitations
            </button>
            <button className={`admin-tab ${activeAdminTab === 'members' ? 'active' : ''}`} onClick={() => setActiveAdminTab(activeAdminTab === 'members' ? null : 'members')}>
              Members
            </button>
            <button className={`admin-tab ${activeAdminTab === 'audit' ? 'active' : ''}`} onClick={() => setActiveAdminTab(activeAdminTab === 'audit' ? null : 'audit')}>
              Audit Logs
            </button>
            <button className={`admin-tab ${activeAdminTab === 'importexport' ? 'active' : ''}`} onClick={() => setActiveAdminTab(activeAdminTab === 'importexport' ? null : 'importexport')}>
              Import/Export
            </button>
          </div>

          {activeAdminTab === 'invitations' && (
            <div style={{ padding: '20px' }}>
              <h3>Invite Member</h3>
              {inviteError && <div className="error-message">{inviteError}</div>}
              {inviteSuccess && <div className="success-message">{inviteSuccess}</div>}
              <form onSubmit={handleInvite} style={{ maxWidth: '400px', marginBottom: '20px' }}>
                <div className="form-group">
                  <label>Email:</label>
                  <input type="email" value={inviteEmail} onChange={(e) => setInviteEmail(e.target.value)} placeholder="member@example.com" required />
                </div>
                <div className="form-group">
                  <label>Role:</label>
                  <select value={inviteRole} onChange={(e) => setInviteRole(e.target.value)}>
                    <option value="Member">Member</option>
                    <option value="Admin">Admin</option>
                  </select>
                </div>
                <button type="submit" className="btn btn-primary">Send Invitation</button>
              </form>

              {orgInvitations.length > 0 && (
                <>
                  <h3>Sent Invitations</h3>
                  <table className="audit-table" style={{ marginTop: '10px' }}>
                    <thead>
                      <tr><th>Email</th><th>Role</th><th>Status</th><th>Date</th></tr>
                    </thead>
                    <tbody>
                      {orgInvitations.map(inv => (
                        <tr key={inv.id}>
                          <td>{inv.email}</td>
                          <td>{inv.role}</td>
                          <td>
                            <span style={{
                              padding: '2px 8px', borderRadius: '4px', fontSize: '12px', color: 'white',
                              backgroundColor: inv.status === 'Pending' ? '#f39c12' : inv.status === 'Accepted' ? '#27ae60' : '#e74c3c'
                            }}>
                              {inv.status}
                            </span>
                          </td>
                          <td>{new Date(inv.createdAt).toLocaleDateString()}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </>
              )}
            </div>
          )}

          {activeAdminTab === 'members' && (
            <div style={{ padding: '20px' }}>
              <h3>Organisation Members</h3>
              <table className="audit-table" style={{ marginTop: '10px' }}>
                <thead>
                  <tr><th>Username</th><th>Email</th><th>Role</th><th>Joined</th></tr>
                </thead>
                <tbody>
                  {members.map(m => (
                    <tr key={m.userId}>
                      <td>{m.username}</td>
                      <td>{m.email}</td>
                      <td>
                        <span style={{
                          padding: '2px 8px', borderRadius: '4px', fontSize: '12px', color: 'white',
                          backgroundColor: m.role === 'OrgAdmin' ? '#3498db' : '#95a5a6'
                        }}>
                          {m.role === 'OrgAdmin' ? 'Admin' : 'Member'}
                        </span>
                      </td>
                      <td>{new Date(m.joinedAt).toLocaleDateString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {activeAdminTab === 'audit' && <AuditLogViewer organisationId={currentOrg.id} />}
          {activeAdminTab === 'importexport' && <ImportExport organisationId={currentOrg.id} />}
        </section>
      )}
    </div>
  );
};

export default DashboardPage;
