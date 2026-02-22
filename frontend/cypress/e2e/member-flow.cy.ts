describe('Member Flow', () => {
  beforeEach(() => {
    cy.visit('/');
  });

  it('should complete full member journey', () => {
    // Register
    const uniqueId = Date.now().toString().slice(-8);
    const username = `testuser${uniqueId}`;
    const email = `test${uniqueId}@example.com`;

    cy.contains('Register').click();
    cy.get('input[name="username"]').type(username);
    cy.get('input[name="email"]').type(email);
    cy.get('input[name="password"]').type('Test123!@#');
    cy.contains('button', 'Register').click();

    // Create organisation
    cy.get('input[placeholder="New organisation name"]').type('My Test Org');
    cy.contains('button', 'Create').click();
    cy.contains('My Test Org').should('be.visible');

    // Create todo
    cy.get('input[placeholder="What needs to be done?"]').type('Write E2E tests');
    cy.contains('button', 'Add Todo').click();
    cy.contains('Write E2E tests').should('be.visible');

    // Sort by priority
    cy.get('.filter-group select').last().invoke('val', 'priority').trigger('change');
    cy.contains('Write E2E tests').should('be.visible');

    // Toggle todo
    cy.contains('Write E2E tests')
      .closest('.todo-item')
      .find('input[type="checkbox"]')
      .click();
    cy.contains('Write E2E tests').should('have.css', 'text-decoration-line', 'line-through');

    // Logout
    cy.contains('Logout').click();
    cy.contains('Login').should('be.visible');

    // Login again
    cy.get('input[name="username"]').type(username);
    cy.get('input[name="password"]').type('Test123!@#');
    cy.contains('button', 'Login').click();
    cy.contains(`Welcome, ${username}`).should('be.visible');
    cy.contains('Write E2E tests').should('be.visible');
  });
});
