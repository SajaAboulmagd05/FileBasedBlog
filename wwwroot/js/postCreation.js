
const MAX_WIDTH = 800; // Maximum allowed width


const uploadBox = document.getElementById("upload-box"),
  previewImg = document.getElementById("preview-img"),
  fileInput = document.getElementById("cover-image"),
  placeholderIcon = document.getElementById("placeholder-icon"),
  uploadMessage = document.getElementById("upload-message");

fileInput.addEventListener("change", (e) => {
  const file = e.target.files[0];
  if (!file) return;

  // Show preview
  previewImg.src = URL.createObjectURL(file);
  previewImg.style.display = "block";

  // Hide placeholder icon/message
  placeholderIcon.style.display = "none";
  uploadMessage.style.display = "none";
});

uploadBox.addEventListener("click", () => fileInput.click());


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

    // Bind post status box click events
    document.querySelectorAll('.stat-card').forEach(card => {
      const classList = card.classList;
      let statusEnum = null;

      if (classList.contains("draft")) statusEnum = 0;
      else if (classList.contains("posted")) statusEnum = 1;
      else if (classList.contains("scheduled")) statusEnum = 2;

      card.addEventListener("click", () => {
        if (statusEnum === null) {
          showToast("error", "Invalid post status");
          return;
        }

        loadPostsByStatus(statusEnum);
        updatePostLabel(getStatusText(statusEnum));

        // Optional: highlight selected card
        document.querySelectorAll(".stat-card").forEach(c => c.classList.remove("active"));
        card.classList.add("active");
      });
    });


    // Trigger default view (e.g. Drafts)
    loadPostsByStatus(0);
    updatePostLabel("Drafts");

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

    // Load tags/categories
    loadTags();
    loadCategories();
  }
}


//update the showing label above the table 
function updatePostLabel(label) {
  document.getElementById("post-type-label").textContent = `Showing: ${label}`;
}

// Hook tab buttons on DOM load
window.addEventListener("DOMContentLoaded", () => {
  document.getElementById("add-btn")?.addEventListener("click", () => navigate("create"));

  // Optional: default to manage tab
  navigate("manage");
});



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
// Make sure you store this during login
function getCurrentUser() {
  return {
    token: localStorage.getItem("authToken"),
    id: localStorage.getItem('userId'), 
    role: localStorage.getItem('userRole'),
    name: localStorage.getItem('userName'),
    initials: localStorage.getItem('userInitials')
  };
}

// Load post statistics
async function loadPostStats() {
  try {
    const user = getCurrentUser();
    const url = user.role === 'Admin'
      ? `/api/posts/stats?all=true`
      : `/api/posts/stats`;

    const response = await fetch(url, {
      headers: { "Authorization": `Bearer ${localStorage.getItem("authToken")}` }
    });
    if (!response.ok) throw new Error('Failed to load stats');

    const stats = await response.json();

  document.querySelector('.stat-card.draft p').textContent = stats.draftPosts || 0;
  document.querySelector('.stat-card.scheduled p').textContent = stats.scheduledPosts || 0;
  document.querySelector('.stat-card.posted p').textContent = stats.publishedPosts || 0;


    return stats;
  } catch (error) {
    console.error('Error loading post stats:', error);
    showToast('Failed to load post statistics', 'error');
    return { drafts: 0, scheduled: 0, posted: 0 };
  }
}

//mapping the vlaues to the enum values
const statusLabels = {
  0: "draft",
  1: "published",
  2: "scheduled"
};

function getStatusText(statusEnum) {
  return statusLabels[statusEnum] || "unknown";
}



// Load posts by status
async function loadPostsByStatus(status) {
  try {
    const user = getCurrentUser();
    const url = user.role === 'Admin'
      ? `/api/posts/status?status=${status}&all=true`
      : `/api/posts/status?status=${status}`;

    const response = await fetch(url, {
      headers: { "Authorization": `Bearer ${localStorage.getItem("authToken")}` }
    });
    if (!response.ok) throw new Error('Failed to load posts');

    const posts = await response.json();
    renderPostsTable(posts);
    //updatePostLabel(status);
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
  const isAdmin = user.role === 'Admin';
  

  if (posts.length === 0) {
    tableBody.innerHTML = '<tr><td colspan="5">No posts found</td></tr>';
    return;
  }
  console.log("Raw posts:", posts);
 
  tableBody.innerHTML = posts.map(post => {
  const statusText = getStatusText(post.status);
  const displayText = statusText.charAt(0).toUpperCase() + statusText.slice(1);

  return `
    <tr class="${isAdmin || post.authorId === user.id ? '' : 'read-only'}">
      <td>${post.title}</td>
      <td>${post.author || 'Unknown'}</td>
      <td>${new Date(post.createdAt).toLocaleDateString()}</td>
      <td>${post.likeCount}</td>
      <td>${post.comments.length}</td>
      <td class="actions">
        ${isAdmin || post.authorId === user.id ? `
          <button class="edit-btn" data-id="${post.id}"><i class="fas fa-edit"></i></button>
          <button class="delete-btn" data-id="${post.id}"><i class="fas fa-trash"></i></button>
        ` : ''}
      </td>
    </tr>
  `;
}).join('');

  
  // Add event listeners to buttons
  // document.querySelectorAll('.edit-btn').forEach(btn => {
  //   btn.addEventListener('click', () => editPost(btn.dataset.id));
  // });
  document.querySelectorAll('.delete-btn').forEach(btn => {
  const postId = btn.dataset.id;
  const post = posts.find(p => p.id === postId); // match the post

  if (post) {
    btn.addEventListener('click', () => openDeleteModal(post));
  }
});

 
}


//update this to be delete post not user 
function openDeleteModal(post) {
  const content = document.getElementById("delete-form-content");
  content.innerHTML = `
    <h2>Confirm Deletion</h2>

    <div className="modal-content">
     
      <p>Are you sure you want to delete <strong>${post.title}</strong>? This action cannot be undone.</p>

        <form id="delete-form">
          <button type="submit" style="background:#f44336;">Yes, Delete</button>
        </form>
    </div>
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
        showToast("success", "Post deleted!");
        document.getElementById("delete-toggle").checked = false;
        
      } else {
        const error = await res.text();
        showToast("error", error);
      }
    } catch {
      showToast("error", "Network error");
    }
  });
}

// Initialize the page
async function initPostManagement() {
  await loadPostStats();
  await loadPostsByStatus(currentFilter);

}

// Update your DOMContentLoaded
document.addEventListener('DOMContentLoaded', () => {
  initPostManagement();
  document.getElementById("add-btn")?.addEventListener("click", () => navigate("create"));
  navigate("manage");
});



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


