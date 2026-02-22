describe('Admin Flow', () => {
  beforeEach(() => {
    cy.visit('/');
  });

  it('should complete admin journey', () => {
    // Register admin
    const uniqueId = Date.now().toString().slice(-8);
    const adminUsername = `admin${uniqueId}`;

    cy.contains('Register').click();
    cy.get('input[name="username"]').type(adminUsername);
    cy.get('input[name="email"]').type(`admin${uniqueId}@example.com`);
    cy.get('input[name="password"]').type('Test123!@#');
    cy.contains('button', 'Register').click();

    // Create organisation
    cy.get('input[placeholder="New organisation name"]').type('Admin Org');
    cy.contains('button', 'Create').click();
    cy.contains('Admin Org').should('be.visible');

    // Add member via admin tools
    cy.contains('Add Member').click();
    cy.get('.add-member-section input[type="email"]').type(`member${uniqueId}@example.com`);
    cy.contains('button', 'Add').click();

    // Create todos
    cy.get('input[placeholder="What needs to be done?"]').type('Admin task 1');
    cy.contains('button', 'Add Todo').click();
    cy.get('input[placeholder="What needs to be done?"]').type('Admin task 2');
    cy.contains('button', 'Add Todo').click();

    // Soft delete
    cy.contains('Admin task 1').closest('.todo-item').find('button[title="Move to trash"]').click();
    cy.contains('Admin task 1').should('not.exist');

    // Show deleted
    cy.contains('Show deleted').click();
    cy.contains('Admin task 1').should('be.visible');

    // Restore
    cy.contains('Admin task 1').closest('.todo-item').find('button[title="Restore"]').click();
    cy.contains('Admin task 1').closest('.todo-item').should('not.have.class', 'deleted');

    // Import/Export - navigate to admin tools
    cy.contains('Import/Export').click();
    cy.contains('Download Template').click();

    // View audit logs
    cy.contains('Audit Logs').click();
    cy.contains('TodoCreated').should('be.visible');
  });
});
