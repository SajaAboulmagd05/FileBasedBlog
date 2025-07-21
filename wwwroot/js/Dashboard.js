// 🧠 Helpers
function capitalize(str) {
  return str.charAt(0).toUpperCase() + str.slice(1);
}

const singularize = {
  users: 'User',
  tags: 'Tag',
  categories: 'Category'
};

// 📦 Sample Data
const users = {
  Admins: [
    { name: 'Alice', email: 'alice@example.com', joined: '2022-03-10' },
    { name: 'Bob', email: 'bob@example.com', joined: '2022-05-22' }
  ],
  Authors: [{ name: 'Tom', email: 'tom@example.com', joined: '2022-01-12' }],
  Editors: [{ name: 'Jane', email: 'jane@example.com', joined: '2023-02-28' }],
  Members: [{ name: 'Mona', email: 'mona@example.com', joined: '2023-07-15' }]
};

const tags = [
  { name: 'JavaScript', associatedPosts: ['p1', 'p2'] },
  { name: 'Design', associatedPosts: ['p3'] }
];

const categories = [
  {
    name: 'Security',
    description: 'Protecting web apps with best practices',
    associatedPosts: ['p1', 'p4']
  },
  {
    name: 'UI/UX',
    description: 'Designing delightful interactions',
    associatedPosts: ['p2']
  }
];

// 🔧 Navigation Logic
function navigate(section) {
  document.getElementById('section-title').textContent = `Manage ${capitalize(section)}`;
  document.getElementById('add-btn').textContent = `Add ${singularize[section] || 'Item'}`;

  // Toggle visibility for all sections
  document.getElementById('user-stats').style.display = section === 'users' ? 'flex' : 'none';
  document.getElementById('user-type-label').style.display = section === 'users' ? 'block' : 'none';
  document.querySelector('.user-table').style.display = section === 'users' ? 'table' : 'none';

  document.getElementById('tags-content').style.display = section === 'tags' ? 'block' : 'none';
  document.getElementById('categories-content').style.display = section === 'categories' ? 'block' : 'none';

  // Render relevant data
  if (section === 'users') {
    filterUsers('Admins');
  } else if (section === 'tags') {
    renderTags();
  } else if (section === 'categories') {
    renderCategories();
  }
}


// 👥 User Renderer
function filterUsers(role) {
  document.getElementById('user-type-label').textContent = `Showing: ${role}`;
  const list = document.getElementById('user-list');
  list.innerHTML = users[role].map(user => `
    <tr>
      <td>${user.name}</td>
      <td>${user.email}</td>
      <td>${user.joined}</td>
      <td>
        <button class="role-btn">Change Role</button>
        <button class="delete-btn">Delete</button>
      </td>
    </tr>
  `).join('');
}

// Tag Renderer
function renderTags() {
  const totalTags = tags.length;
  const totalPosts = tags.reduce((sum, tag) => sum + tag.associatedPosts.length, 0);

  document.getElementById('tag-stats').innerHTML = `
    <div class="stat-card" style="background-color:#f0d9ff;">
      <h3>Total Tags</h3><p>${totalTags}</p>
    </div>
    <div class="stat-card" style="background-color:#d0f0ff;">
      <h3>Total Tag Posts</h3><p>${totalPosts}</p>
    </div>
  `;

  document.getElementById('tag-list').innerHTML = tags.map(tag => `
    <tr>
      <td>${tag.name}</td>
      <td>${tag.associatedPosts.length}</td>
      <td>
        <button class="role-btn">Edit</button>
        <button class="delete-btn">Delete</button>
      </td>
    </tr>
  `).join('');
}

// Category Renderer
function renderCategories() {
  const totalCategories = categories.length;
  const totalPosts = categories.reduce((sum, c) => sum + c.associatedPosts.length, 0);

  document.getElementById('category-stats').innerHTML = `
    <div class="stat-card" style="background-color:#d7f7d7;">
      <h3>Total Categories</h3><p>${totalCategories}</p>
    </div>
    <div class="stat-card" style="background-color:#fdf6c5;">
      <h3>Total Category Posts</h3><p>${totalPosts}</p>
    </div>
  `;

  document.getElementById('category-list').innerHTML = categories.map(c => `
    <tr>
      <td>${c.name}</td>
      <td>${c.associatedPosts.length}</td>
      <td>${c.description}</td>
      <td>
        <button class="role-btn">Edit</button>
        <button class="delete-btn">Delete</button>
      </td>
    </tr>
  `).join('');
}

function openModal(type) {
  const modalContent = document.getElementById("modal-form-content");
  document.getElementById("modal-toggle").checked = true;

  let formHTML = '';

  if (type === 'user') {
    formHTML = `
      <h2>Create New User</h2>
      <form id="create-user">
        <input type="email" name="email" placeholder="Email address" required />
        <input type="text" name="name" placeholder="Name" required />
        <select name="role" required>
            <option value="" disabled selected>Select Role</option>
            <option value="Admin">Admin</option>
            <option value="Author">Author</option>
            <option value="Editor">Editor</option>
            <option value="Member">Member</option>
        </select>
        <input type="password" name="password" placeholder="Password" required />
        <label><input type="checkbox" name="newsletter" /> Subscribe to newsletter</label>
        <button type="submit">Create User</button>
      </form>
    `;
  } else if (type === 'category') {
    formHTML = `
      <h2>Create New Category</h2>
      <form id="create-category">
        <input type="text" name="name" placeholder="Category Name" required />
        <input type="text" name="description" placeholder="Short Description" required />
        <button type="submit">Create Category</button>
      </form>
    `;
  } else if (type === 'tag') {
    formHTML = `
      <h2>Create New Tag</h2>
      <form id="create-tag">
        <input type="text" name="name" placeholder="Tag Name" required />
        <button type="submit">Create Tag</button>
      </form>
    `;
  }

  modalContent.innerHTML = formHTML;
}

//  Initialize default view
navigate('users');

document.getElementById('add-btn').addEventListener('click', function () {
  // Determine which section is currently active
   const currentSection = document.getElementById('section-title').textContent.toLowerCase().replace('manage ', '');

  if (currentSection.includes('user')) {
    openModal('user');
  } else if (currentSection.includes('tag')) {
    openModal('tag');
  } else if (currentSection.includes('categor')) {
    openModal('category');
  }
});

