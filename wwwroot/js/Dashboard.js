// 🧠 Helpers
function capitalize(str) {
  return str.charAt(0).toUpperCase() + str.slice(1);
}

const singularize = {
  users: 'User',
  tags: 'Tag',
  categories: 'Category'
};
const pluralize = {
  user: 'users',
  tag: 'tags',
  category: 'categories'
};

let activeSection = 'users'; // tracks current section for modal

// Navigation Logic
function navigate(section) {
  const user = getCurrentUser(); // from session or JWT


    if (!user || user.role !== "Admin") {
      localStorage.setItem("toastMessage", "Unauthorized access");
      window.location.href = "/";

    }
  activeSection = section;

  document.getElementById('section-title').textContent = `Manage ${capitalize(section)}`;
  document.getElementById('add-btn').textContent = `Add ${singularize[section] || 'Item'}`;

  // Toggle visibility for all sections
  document.getElementById('user-stats').style.display = section === 'users' ? 'flex' : 'none';
  document.getElementById('user-type-label').style.display = section === 'users' ? 'block' : 'none';
  document.querySelector('.user-table').style.display = section === 'users' ? 'table' : 'none';

  document.getElementById('tags-content').style.display = section === 'tags' ? 'block' : 'none';
  document.getElementById('categories-content').style.display = section === 'categories' ? 'block' : 'none';

  // Load content
  if (section === 'users') {
    fetch("/api/users/counts")
      .then(res => res.json())
      .then(counts => {
        const statsContainer = document.getElementById("user-stats");
        statsContainer.innerHTML = "";

        for (const role in counts) {
          const box = document.createElement("div");
          const classMap = {
              Admins: "admin",
              Authors: "author",
              Editors: "editor",
              Members: "member"
            };
          box.className = `stat-card ${classMap[role] || "default"}`;
          box.innerHTML = `<h3>${role}</h3><p>${counts[role]}</p>`;
          box.addEventListener("click", () => fetchUsers(role.slice(0, -1))); // 'Admins' → 'Admin'
          statsContainer.appendChild(box);
        }

        fetchUsers("Admin"); // default view
      })
      .catch(() => showToast("error", "Unable to fetch user stats"));
  } else if (section === 'tags') {
    renderTags();
  } else if (section === 'categories') {
    renderCategories();
  }
}

// User Renderer
function fetchUsers(role) {
  document.getElementById('user-type-label').textContent = `Showing: ${role}s`;

  fetch(`/api/users/by-role/${role}`)
    .then(res => res.json())
    .then(users => {
      const list = document.getElementById('user-list');
      list.innerHTML = users.map(user => `
        <tr>
          <td>${user.name}</td>
          <td>${user.email}</td>
          <td>${new Date(user.createdDate).toLocaleDateString()}</td>
          <td><div class="centered">${user.isEmailVerified 
            ? '<i class="fas fa-check-circle" style="font-size: 1.2rem; color:green;"></i>' 
            : '<i class="fas fa-times-circle" style="font-size: 1.2rem; color:red;"></i>'}
          </div></td>

          <td><div class="centered">${user.isSubscribedToNewsletter 
            ? '<i class="fas fa-check-circle" style="font-size: 1.2rem; color:green;"></i>' 
            : '<i class="fas fa-times-circle" style="font-size: 1.2rem; color:red;"></i>'}
          </div><td>
            <button class="role-btn" onclick="openRoleModal('${user.email}', '${user.role}')">Change Role</button>
            <button class="delete-btn" onclick="openDeleteModal('${user.email}')">Delete</button>
          </td>
        </tr>
      `).join('');
    })
    .catch(() => showToast("error", "Unable to fetch users"));
}
const token = localStorage.getItem("authToken");

// Tag Renderer
function renderTags() {
    fetch("/api/tags/all", {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })
    .then(res => res.json())
    .then(tags => {
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
      tags.sort((a, b) => a.name.localeCompare(b.name));
      document.getElementById('tag-list').innerHTML = tags.map(tag => `
        <tr>
          <td>${tag.name}</td>
          <td>
            <div class="posts-cell">
              <span class="post-count">${tag.associatedPosts.length}</span>
              <!-- <button class="show-posts-btn" >Show Posts</button>-->
            </div>
          </td>
          <td>
            <button class="role-btn">Edit</button>
            <button class="delete-btn">Delete</button>
          </td>
        </tr>
      `).join('');
          // Bind Edit/Delete buttons for tags
    document.querySelectorAll("#tag-list .role-btn").forEach((btn, i) => {
      btn.addEventListener("click", () => openEditModal("tag", tags[i]));
    });
    document.querySelectorAll("#tag-list .delete-btn").forEach((btn, i) => {
      btn.addEventListener("click", () => openTagCategoryDeleteModal("tag", tags[i]));
    });

    });
}

// Category Renderer
function renderCategories() {
      fetch("/api/categories/all", {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })
    .then(res => res.json())
    .then(categories => {
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
      categories.sort((a, b) => a.name.localeCompare(b.name));
      document.getElementById('category-list').innerHTML = categories.map(c => `
        <tr>
          <td>${c.name}</td>
          <td>
            <div class="posts-cell">
              <span class="post-count">${c.associatedPosts.length}</span>
              <!--<button class="show-posts-btn" >Show Posts</button>-->
            </div>
          </td>
          <td>
            <button class="role-btn">Edit</button>
            <button class="delete-btn">Delete</button>
          </td>
        </tr>
      `).join('');
      // Bind Edit/Delete buttons for categories
      document.querySelectorAll("#category-list .role-btn").forEach((btn, i) => {
        btn.addEventListener("click", () => openEditModal("category", categories[i]));
      });
      document.querySelectorAll("#category-list .delete-btn").forEach((btn, i) => {
        btn.addEventListener("click", () => openTagCategoryDeleteModal("category", categories[i]));
      });

    });
}

// Modal Logic
function openModal(type) {
  console.log(`Opening modal for type: ${type}, activeSection: ${activeSection}`);
  const modalContent = document.getElementById("modal-form-content");
  modalContent.innerHTML = "";
  document.getElementById("modal-toggle").checked = true;

  let formHTML = "";

  if (singularize[type].toLowerCase() === 'user') {
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
  } else if (singularize[type].toLowerCase() === 'category') {
    formHTML = `
      <h2>Create New Category</h2>
      <form id="create-category">
        <input type="text" name="name" placeholder="Category Name" required />
        <textarea type="text" name="description" placeholder="Short Description" class="multi-line-input" required></textarea>
        <!--<input type="text" name="description" placeholder="Short Description" required />-->
        <button type="submit">Create Category</button>
      </form>
    `;
  } else if (singularize[type].toLowerCase() === 'tag') {
    formHTML = `
      <h2>Create New Tag</h2>
      <form id="create-tag">
        <input type="text" name="name" placeholder="Tag Name" required />
        <button type="submit">Create Tag</button>
      </form>
    `;
  }

  modalContent.innerHTML = formHTML;

  const form = modalContent.querySelector("form");
  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const formData = new FormData(form);

    let endpoint = "";
    if (singularize[type].toLowerCase() === "user") endpoint = "/api/users/admin-add";
    if (singularize[type].toLowerCase() === "tag") endpoint = "/api/tags/add";
    if (singularize[type].toLowerCase() === "category") endpoint = "/api/categories/add";

    try {
      const res = await fetch(endpoint, {
      method: "POST",
      body: formData,
      headers: {
        "Authorization": `Bearer ${localStorage.getItem("authToken")}`
      }
    });

      if (res.ok) {
        showToast("success", `${singularize[type]} created!`);
        form.reset();
        document.getElementById("modal-toggle").checked = false;
        if (singularize[type].toLowerCase() === "user") {
            navigate("users");
            const role = formData.get("role");
            const statCard = document.querySelector(`.stat-card.${role.toLowerCase()}`);
            const countElem = statCard.querySelector("p");
            countElem.textContent = parseInt(countElem.textContent) + 1;
            const list = document.getElementById("user-list");
            const email = formData.get("email");
            const name = formData.get("name");
            const joined = new Date().toLocaleDateString();

            const newRow = document.createElement("tr");
            newRow.innerHTML = `
              <td>${name}</td>
              <td>${email}</td>
              <td>${joined}</td>
              <td>
                <button class="role-btn" onclick="openRoleModal('${email}', '${role}')">Change Role</button>
                <button class="delete-btn" onclick="openDeleteModal('${email}')">Delete</button>
              </td>
            `;
            if (document.getElementById("user-type-label").textContent.includes(role)) {
              list.appendChild(newRow);
            }


        }
          
        if (singularize[type].toLowerCase() === "tag") navigate("tags");
        if (singularize[type].toLowerCase() === "category") navigate("categories");
      } else {
        const error = await res.text();
        showToast("error", error);
      }
    } catch {
      showToast("error", "Network error");
    }
  });
}
// Default View + Add Button Logic
navigate("users");

document.getElementById("add-btn").addEventListener("click", () => {
  openModal(activeSection);
});
//  Toast Logic
function showToast(type, message = "") {
  const toast = document.getElementById(`${type}-toast`);
  if (message) toast.querySelector(".message").textContent = message;

  toast.classList.remove("hidden");
  toast.classList.add("visible");

  setTimeout(() => {
    toast.classList.remove("visible");
    toast.classList.add("hidden");
  }, 3000);
}



function openRoleModal(email, currentRole) {
  const modalContent = document.getElementById("modal-form-content");
  modalContent.innerHTML = `
    <h2>Change Role for ${email}</h2>
    <form id="change-role-form">
      <select name="role" required>
        <option value="" disabled>Select New Role</option>
        <option value="Admin">Admin</option>
        <option value="Author">Author</option>
        <option value="Editor">Editor</option>
        <option value="Member">Member</option>
      </select>
      <input type="hidden" name="email" value="${email}" />
      <button type="submit">Update Role</button>
    </form>
  `;
  document.getElementById("modal-toggle").checked = true;

  const form = modalContent.querySelector("form");
  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const formData = new FormData(form);

    try {
      const res = await fetch("/api/users/change-role", {
        method: "POST",
        body: formData
      });

      if (res.ok) {
        showToast("success", "Role updated!");
        document.getElementById("modal-toggle").checked = false;
        navigate("users");
      } else {
        const error = await res.text();
        showToast("error", error);
      }
    } catch {
      showToast("error", "Network error");
    }
  });
}

function openDeleteModal(email) {
  const content = document.getElementById("delete-form-content");
  content.innerHTML = `
    <h2>Confirm Deletion</h2>
    <p>Are you sure you want to delete the account of: <strong>${email}</strong>?</p>
    <form id="delete-form">
      <input type="hidden" name="email" value="${email}" />
      <button type="submit" style="background:#f44336;">Yes, Delete</button>
    </form>
  `;
  document.getElementById("delete-toggle").checked = true;

  const form = document.getElementById("delete-form");
  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const formData = new FormData(form);

    try {
      const res = await fetch("/api/users/delete", {
        method: "POST",
        body: formData
      });

      if (res.ok) {
        showToast("success", "User deleted!");
        document.getElementById("delete-toggle").checked = false;
        navigate("users");
      } else {
        const error = await res.text();
        showToast("error", error);
      }
    } catch {
      showToast("error", "Network error");
    }
  });
}

function parseToken(token) {
  try {
    return JSON.parse(atob(token.split('.')[1]));
  } catch {
    return {};
  }
}

//rendering of nav menu according to user intials 
function getCurrentUser() {
  return {
    token: localStorage.getItem("authToken"),
    id: localStorage.getItem('userId'), 
    role: localStorage.getItem('userRole'),
    name: localStorage.getItem('userName'),
    initials: localStorage.getItem('userInitials')
  };
}

document.addEventListener("DOMContentLoaded", () => {
   const user = getCurrentUser();
  // Show avatar + initials
  const avatar = document.getElementById("user-menu");
  document.getElementById("user-avatar").textContent = user.initials;
  avatar.classList.remove("hidden");
  if(user.role=="Admin")
  document.getElementById("dashboard-link").classList.remove("hidden");
  document.getElementById("postManage-link").classList.remove("hidden");
});


//handle the branding menu 
document.addEventListener("DOMContentLoaded", () => {
  const avatar = document.getElementById("user-avatar");
  const dropdown = document.getElementById("user-dropdown");

  // Toggle dropdown on avatar click
  avatar.addEventListener("click", () => {
    dropdown.classList.toggle("hidden");
  });

  // Close dropdown if clicked elsewhere
  document.addEventListener("click", (e) => {
    if (!avatar.contains(e.target) && !dropdown.contains(e.target)) {
      dropdown.classList.add("hidden");
    }
  });
});


//edit modal for tags and categories
function openEditModal(type, item) {
  const modalContent = document.getElementById("modal-form-content");
  document.getElementById("modal-toggle").checked = true;

  let formHTML = "";

  if (type === "tag") {
    formHTML = `
      <h2>Edit Tag</h2>
      <form id="edit-tag-form">
        <input type="hidden" name="id" value="${item.id}" />
        <input type="text" name="name" placeholder="Tag Name" value="${item.name}" required />
        <button type="submit">Update Tag</button>
      </form>
    `;
  } else if (type === "category") {
    formHTML = `
      <h2>Edit Category</h2>
      <form id="edit-category-form">
        <input type="hidden" name="id" value="${item.id}" />
        <label for="categoryName">Category Name</label>
        <input type="text" name="name" id="categoryName" placeholder="Category Name" value="${item.name}" required />
        <label for="editDescription">Description</label>
        <textarea name="description" id="editDescription" placeholder="Description" class="multi-line-input" required>${item.description}</textarea>
        <button type="submit">Update Category</button>
      </form>
    `;
  }

  modalContent.innerHTML = formHTML;

  const form = modalContent.querySelector("form");
  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const formData = new FormData(form);

    const endpoint =
      type === "tag" ? "/api/tags/update" :
      type === "category" ? "/api/categories/update" : "";

    try {
      const res = await fetch(endpoint, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${localStorage.getItem("authToken")}`
        },
        body: formData
      });

      if (res.ok) {
        showToast("success", `${type.charAt(0).toUpperCase() + type.slice(1)} updated!`);
        document.getElementById("modal-toggle").checked = false;
        navigate(pluralize[type]);
      } else {
        const error = await res.text();
        showToast("error", error);
      }
    } catch {
      showToast("error", "Network error");
    }
  });
}


//delete modal for tags and categories 
function openTagCategoryDeleteModal(type, item) {
  const content = document.getElementById("delete-form-content");
  document.getElementById("delete-toggle").checked = true;

  content.innerHTML = `
    <h2>Confirm Deletion</h2>
    <p>Delete ${type}: <strong>${item.name}</strong>?</p>
    <form id="delete-form">
      <input type="hidden" name="id" value="${item.id}" />
      <button type="submit" style="background:#f44336;">Yes, Delete</button>
    </form>
  `;

  const form = content.querySelector("form");
  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const formData = new FormData(form);

    const endpoint =
      type === "tag" ? "/api/tags/delete" :
      type === "category" ? "/api/categories/delete" : "";

    try {
      const res = await fetch(endpoint, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${localStorage.getItem("authToken")}`
        },
        body: formData
      });

      if (res.ok) {
        showToast("success", `${type.charAt(0).toUpperCase() + type.slice(1)} deleted!`);
        document.getElementById("delete-toggle").checked = false;
        navigate(pluralize[type]);
      } else {
        const error = await res.text();
        showToast("error", error);
      }
    } catch {
      showToast("error", "Network error");
    }
  });
}

//logout handler 
document.getElementById("logout-btn").addEventListener("click", () => {
    localStorage.clear();
    window.location.href = "/";
  });