const uploadBox = document.querySelector(".upload-box"),
previewImg = uploadBox.querySelector("img"),
fileInput = uploadBox.querySelector("input"),
widthInput = document.querySelector(".width input"),
heightInput = document.querySelector(".height input"),
aspectRatioCheckbox = document.querySelector("#aspect-ratio-checkbox");

const MAX_WIDTH = 800; // Maximum allowed width


// Toggle between Manage Posts and Create Post
function navigate(section) {
  const stats = document.getElementById("post-stats");
  const label = document.getElementById("post-type-label");
  const table = document.querySelector(".user-table");
  const createSection = document.getElementById("create-post-section");
  const sectionTitle = document.getElementById("section-title");
  

  if (section === "manage") {
    stats.style.display = "flex";
    label.style.display = "block";
    table.style.display = "table";
    createSection.style.display = "none";
    sectionTitle.textContent = "Manage Posts";
   
  } else if (section === "create") {
    stats.style.display = "none";
    label.style.display = "none";
    table.style.display = "none";
    createSection.style.display = "block";
    sectionTitle.textContent = "Create Post";
  

    // Initialize EasyMDE once
    if (!window.easyMDE) {
      window.easyMDE = new EasyMDE({
        element: document.getElementById("post-body"),
        spellChecker: false,
        placeholder: "Write your post content in Markdown...",
        autosave: {
          enabled: true,
          uniqueId: "post-body-autosave",
          delay: 1000
        },
        status: false,
        imageUpload: true,
        imageMaxSize: 2 * 1024 * 1024,
        imageAccept: "image/*",
        uploadImage: true,
        imageUploadEndpoint: "/api/uploads/markdown-image",
        promptURLs: true
      });
    }

    // ✅ Load tags/categories
    loadTags();
    loadCategories();
  }
}

// Hook tab buttons on DOM load
window.addEventListener("DOMContentLoaded", () => {
  document.getElementById("add-btn")?.addEventListener("click", () => navigate("create"));

  // Optional: default to manage tab
  navigate("manage");
});


const loadFile = (e) => {
    const file = e.target.files[0]; 
    if (!file) return;

    previewImg.src = URL.createObjectURL(file); 
    previewImg.addEventListener("load", () => {
        let originalWidth = previewImg.naturalWidth;
        let originalHeight = previewImg.naturalHeight;

        // Apply max width constraint
        if (originalWidth > MAX_WIDTH) {
            let scaleFactor = MAX_WIDTH / originalWidth;
            originalWidth = MAX_WIDTH;
            originalHeight = Math.floor(originalHeight * scaleFactor);
        }


        document.querySelector(".wrapper")?.classList.add("active");
    });
};

fileInput.addEventListener("change", loadFile);
uploadBox.addEventListener("click", () => fileInput.click());





//the scheduling time and date options 
document.addEventListener("DOMContentLoaded", function() {
    const scheduleOption = document.querySelector('input[name="publish-option"][value="schedule"]');
    const immediateOption = document.querySelector('input[name="publish-option"][value="immediate"]');
    const scheduleContainer = document.getElementById("schedule-container");

    scheduleOption.addEventListener("change", function() {
        scheduleContainer.style.display = "block";
    });

    immediateOption.addEventListener("change", function() {
        scheduleContainer.style.display = "none";
    });
});

//trying to load the tags 
async function loadTags() {
    try {
        const response = await fetch('/api/tags');

        if (!response.ok) {
            throw new Error(`Error loading tags: ${response.statusText}`);
        }

        const tags = await response.json();
        const tagSelect = document.getElementById('post-tags');
        tags.sort((a, b) => a.Name.localeCompare(b.Name));
        // Clear existing options
        tagSelect.innerHTML = '';
        
        console.log("Tags received:", tags);
        // Populate new options
        tags.forEach(tag => {
            const option = document.createElement('option');
            option.value = tag.Name || tag.name; // in case it's lowercase
            option.textContent = tag.Name || tag.name;
            tagSelect.appendChild(option);
        });
    } catch (error) {
        console.error('Failed to load tags:', error);
    }
}

//trying to load the tags 
async function loadCategories() {
    try {
        const response = await fetch('/api/categories');

        if (!response.ok) {
            throw new Error(`Error loading tags: ${response.statusText}`);
        }

        const categories = await response.json();
        const categorySelect = document.getElementById('post-categories');
        categories.sort((a, b) => a.Name.localeCompare(b.Name));
        // Clear existing options
        categorySelect.innerHTML = '';
        console.log("Categories received:", categories);

        // Populate new options
        categories.forEach(category => {
            const option = document.createElement('option');
            option.value = category.Name || category.name; // in case it's lowercase
            option.textContent = category.Name || category.name;
            categorySelect.appendChild(option);
        });
    } catch (error) {
        console.error('Failed to load categories :', error);
    }
}
// Load tags once the DOM is ready
document.addEventListener('DOMContentLoaded', loadTags);
document.addEventListener('DOMContentLoaded', loadCategories);


//saving the post as draft 
document.getElementById("save-draft").addEventListener("click", async function () {
    const form = new FormData(document.getElementById("post-form"));
   form.set("body", easyMDE.value()); 
   
    // Validate required fields
    const title = form.get("title")?.toString().trim();
    const description = form.get("description")?.toString().trim();
    const body = form.get("body")?.toString().trim();
    const coverImage = form.get("coverImage");

    if (!title || !description || !body) {
        alert("Title, description, and content are required.");
        return;
    }

    if (!coverImage) {
        alert("Please upload a cover image.");
        return;
    }

    try {
        const response = await fetch("/api/posts/draft", {
            method: "POST",
            body: form  ,
            headers: {
          "Authorization": `Bearer ${localStorage.getItem("authToken")}`
        },
        });

        const result = await response.json();
        showToast("success", `Post Drafted!`);
           this.reset(); 
        easyMDE.value("");
    } catch (err) {
        console.error("Failed to save draft:", err);
        alert("Draft save failed.");
    }
});

//submit the post (create it)
document.getElementById("post-form").addEventListener("submit", async function (e) {
    e.preventDefault();

    // Create FormData from the form
    const form = new FormData(this);
    
    // Add the markdown content
    form.set("body", easyMDE.value());

    // Handle scheduling
    const publishOption = form.get("publish-option");
    if (publishOption === "schedule") {
        const dateValue = document.getElementById("publish-date").value;
        const timeValue = document.getElementById("publish-time").value;

        if (!dateValue || !timeValue) {
            alert("Please select both a publish date and time.");
            return;
        }

        const scheduled = new Date(`${dateValue}T${timeValue}`);
        const now = new Date();

        if (scheduled <= now) {
            alert("Scheduled time must be in the future.");
            return;
        }

        form.set("publish-date", dateValue);
        form.set("publish-time", timeValue);
    }

    // Validate required fields
    const title = form.get("title")?.toString().trim();
    const description = form.get("description")?.toString().trim();
    const body = form.get("body")?.toString().trim();
    const coverImage = form.get("coverImage");

    if (!title || !description || !body) {
        alert("Title, description, and content are required.");
        return;
    }

    if (!coverImage) {
        alert("Please upload a cover image.");
        return;
    }

    try {
        const response = await fetch("/api/posts/publish", {
            method: "POST",
            body: form,
            headers: {
          "Authorization": `Bearer ${localStorage.getItem("authToken")}`
        },
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(error || "Failed to publish post");
        }

        const result = await response.json();
        showToast("success", `Post created!`);

       this.reset(); 
        easyMDE.value("");  
    } catch (error) {
        console.error("Submission failed:", error);
        alert("Error: " + error.message);
    }
});

// Global variables
let currentFilter = 'Draft';

// Get user data from localStorage
function getCurrentUser() {
  return {
    id: localStorage.getItem('userId'), // Make sure you store this during login
    role: localStorage.getItem('userRole'),
    name: localStorage.getItem('userName'),
    initials: localStorage.getItem('userInitials')
  };
}

// Load post statistics
async function loadPostStats() {
  try {
    const user = getCurrentUser();
    const url = user.role === 'admin'
      ? `/api/posts/stats?all=true`
      : `/api/posts/stats`;

    const response = await fetch(url, {
      headers: { "Authorization": `Bearer ${localStorage.getItem("authToken")}` }
    });
    if (!response.ok) throw new Error('Failed to load stats');

    const stats = await response.json();

    document.querySelector('.stat-card.draft p').textContent = stats.drafts || 0;
    document.querySelector('.stat-card.scheduled p').textContent = stats.scheduled || 0;
    document.querySelector('.stat-card.posted p').textContent = stats.posted || 0;

    return stats;
  } catch (error) {
    console.error('Error loading post stats:', error);
    showToast('Failed to load post statistics', 'error');
    return { drafts: 0, scheduled: 0, posted: 0 };
  }
}

// Load posts by status
async function loadPostsByStatus(status) {
  try {
    const user = getCurrentUser();
    const url = user.role === 'admin'
      ? `/api/posts/status?status=${status}&all=true`
      : `/api/posts/status?status=${status}`;

    const response = await fetch(url, {
      headers: { "Authorization": `Bearer ${localStorage.getItem("authToken")}` }
    });
    if (!response.ok) throw new Error('Failed to load posts');

    const posts = await response.json();
    renderPostsTable(posts);
    updatePostLabel(status);
    currentFilter = status;

    return posts;
  } catch (error) {
    console.error(`Error loading ${status} posts:`, error);
    showToast(`Failed to load ${status.toLowerCase()} posts`, 'error');
    return [];
  }
}


// Render posts in the table with authorization checks
function renderPostsTable(posts) {
  const tableBody = document.getElementById('post-list');
  const user = getCurrentUser();
  const isAdmin = user.role === 'admin';
  
  if (posts.length === 0) {
    tableBody.innerHTML = '<tr><td colspan="5">No posts found</td></tr>';
    return;
  }
  
  tableBody.innerHTML = posts.map(post => `
    <tr class="${isAdmin || post.authorId === user.id ? '' : 'read-only'}">
      <td>${post.title}</td>
      <td>${post.author || 'Unknown'}</td>
      <td>${formatDate(post.date)}</td>
      <td><span class="status-badge ${post.status.toLowerCase()}">${post.status}</span></td>
      <td class="actions">
        ${isAdmin || post.authorId === user.id ? `
          <button class="edit-btn" data-id="${post.id}"><i class="fas fa-edit"></i></button>
          <button class="delete-btn" data-id="${post.id}"><i class="fas fa-trash"></i></button>
        ` : ''}
      </td>
    </tr>
  `).join('');
  
  // Add event listeners to buttons
  document.querySelectorAll('.edit-btn').forEach(btn => {
    btn.addEventListener('click', () => editPost(btn.dataset.id));
  });
  
  document.querySelectorAll('.delete-btn').forEach(btn => {
    btn.addEventListener('click', () => confirmDeletePost(btn.dataset.id));
  });
}

// Initialize the page
async function initPostManagement() {
  await loadPostStats();
  await loadPostsByStatus(currentFilter);
  
  // Add event listeners to stat cards
  document.querySelectorAll('.stat-card').forEach(card => {
    const status = card.classList.contains('draft') ? 'Draft' :
                  card.classList.contains('scheduled') ? 'Scheduled' : 'Posted';
    card.addEventListener('click', () => filterPosts(status));
  });
}

// Update your DOMContentLoaded
document.addEventListener('DOMContentLoaded', () => {
  initPostManagement();
  document.getElementById("add-btn")?.addEventListener("click", () => navigate("create"));
  navigate("manage");
});

function formatDate(dateStr) {
  const date = new Date(dateStr);
  const options = { year: 'numeric', month: 'short', day: 'numeric' };
  return date.toLocaleDateString(undefined, options);
}


// 🍞 Toast Logic
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
