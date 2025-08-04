
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
  const user = getCurrentUser(); // from session or JWT
   if (user.id==null || user.role == "Member") {
      localStorage.setItem("toastMessage", "Unauthorized access");
      window.location.href = "/";

    }
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
    
     const form = document.getElementById("post-form");

  // Clear all fields
  form.reset();
  window.easyMDE?.value(""); // Clear markdown editor
    // Wipe autosave + preview content
  if (window.easyMDE?.options?.autosave?.uniqueId) {
    localStorage.removeItem("easyMDE-autosave-" + window.easyMDE.options.autosave.uniqueId);
  }

  // Clear preview layer manually
  const livePreview = document.querySelector('.editor-preview');
  if (livePreview) {
    livePreview.innerHTML = ""; // Wipe ghost content
  }
  document.querySelectorAll(".full-preview-modal").forEach(modal => modal.remove());
  document.getElementById("preview-img").style.display = "none";
  document.getElementById("placeholder-icon").style.display = "block";
  document.getElementById("upload-message").style.display = "block";

  // Remove attachments container if it exists
  const attachments = document.querySelector(".existing-attachments");
  if (attachments) attachments.remove();

  // Reset form state
  delete form.dataset.editSlug;
  document.getElementById("section-title").textContent = "Manage Posts";
  document.getElementById("save-draft").textContent = "Save Draft";
  document.getElementById("publish-post").textContent = "Publish Post";
    // Trigger default view (e.g. Drafts)
    loadPostsByStatus(0);
    updatePostLabel("Drafts");
    
  } else if (section === "create") {
    stats.style.display = "none";
    label.style.display = "none";
    table.style.display = "none";
    createSection.style.display = "block";
    sectionTitle.textContent = "Create Post";

    // Initialize EasyMDE with enhanced preview
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
        previewClass: "post-body", // Applies your post styling
        renderingConfig: {
          codeSyntaxHighlighting: true // Enable syntax highlighting
        },
        toolbar: [
          "bold", "italic", "heading", "|",
          "quote", "unordered-list", "ordered-list", "|",
          "link", "image", "|", "side-by-side", "fullscreen", "|",
          {
            name: "enhanced-preview",
            action: function customPreview(editor) {
            // Create modal preview with full post styling
            const modal = document.createElement('div');
            modal.className = 'full-preview-modal';
            modal.innerHTML = `
              <div class="modal-content">
                <button class="close-preview">&times;</button>
                <div class="post-preview post-body"></div>
              </div>
            `;

            document.body.appendChild(modal);

            // Render raw Markdown using EasyMDE's built-in preview renderer
            const rawHTML = editor.options.previewRender(editor.value(), editor);
            const previewDiv = modal.querySelector('.post-preview');
            previewDiv.innerHTML = rawHTML;

            // Ensure images are displayed correctly with fallback styling
            previewDiv.querySelectorAll('img').forEach(img => {
              img.onload = () => {
                img.style.display = 'block';
                img.style.maxWidth = '100%';
                img.style.height = 'auto';
              };
              img.onerror = () => {
              img.src =
                "data:image/svg+xml;base64," +
                btoa(`<svg xmlns='http://www.w3.org/2000/svg' width='120' height='90'><rect width='100%' height='100%' fill='#ccc'/><text x='50%' y='50%' dominant-baseline='middle' text-anchor='middle' fill='#333' font-size='12'>Image not found</text></svg>`);
              img.alt = "Image not found";
              img.style.opacity = '0.6';
            };

            });

            // Apply Prism syntax highlighting
            setTimeout(() => {
              Prism.highlightAllUnder(previewDiv);
            }, 10);

            // Close handler
            modal.querySelector('.close-preview').addEventListener('click', () => {
              modal.remove();
            });
          },

            className: "fa fa-eye",
            title: "Enhanced Preview"
          }
        ],
        status: false,
        imageUpload: true,
        imageMaxSize: 2 * 1024 * 1024,
        imageAccept: "image/*",
        uploadImage: true,
        promptURLs: true,
        imageUploadFunction: function(file, onSuccess, onError) {
          const reader = new FileReader();
          reader.onloadend = () => {
            const base64Image = reader.result;
            const filename = file.name.replace(/\s+/g, "-").toLowerCase();
            const markdownImage = `![${filename}](${base64Image})`;
            onSuccess(markdownImage);
          };
          reader.onerror = () => onError("Image upload failed.");
          reader.readAsDataURL(file);
        }
      });

      // Update preview when content changes
      window.easyMDE.codemirror.on("change", function() {
        setTimeout(() => {
          const preview = document.querySelector('.editor-preview-active');
          if (preview) {
            Prism.highlightAllUnder(preview);
          }
        }, 100);
      });
    }

    // Load tags/categories
    loadTags();
    loadCategories();
  }
}

// Add CSS for the full preview modal if not already added
if (!document.getElementById('preview-modal-style')) {
  const style = document.createElement('style');
  style.id = 'preview-modal-style';
  style.textContent = `
    .full-preview-modal {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0,0,0,0.9);
      z-index: 1000;
      overflow-y: auto;
      padding: 2rem;
    }
    .full-preview-modal .modal-content {
      max-width: 800px;
      margin: 2rem auto;
      background: white;
      padding: 2rem;
      border-radius: 0.5rem;
      position: relative;
    }
    .full-preview-modal .close-preview {
      position: absolute;
      top: 1rem;
      right: 1rem;
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: var(--dark-blue);
    }
    .full-preview-modal .close-preview:hover {
      color: var(--light-blue);
    }
    #post-body + .EasyMDEContainer .editor-preview {
      background: white;
      padding: 2rem;
      border-radius: 0.5rem;
      font-family: homefont, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
      line-height: 1.7;
      color: #333;
    }
  `;
  document.head.appendChild(style);
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
    
    
    if (!title || !description || !body) {
        showToast("error","Title, description, and content are required.");
        return;
    }
   
    const coverImageInput = document.getElementById("cover-image");
    if (!validateCoverImage(coverImageInput)) {
        showToast("error","Please upload a cover image.");
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

        if (!response.ok) {
            const errorText = await response.text(); // fallback to plain text
            throw new Error(errorText || "Failed to save draft");
          }

          const result = await response.json();
        
        showToast("success", `Post Drafted!`);
        document.getElementById("post-form").reset(); 
        easyMDE.value("");
        localStorage.removeItem("smde_post-body-autosave");
        
    } catch (err) {
        console.error("Failed to save draft:", err);
        showToast("error","Draft save failed.");
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
            showToast("error","Please select both a publish date and time.");
            return;
        }

        const scheduled = new Date(`${dateValue}T${timeValue}`);
        const now = new Date();

        if (scheduled <= now) {
            showToast("error","Scheduled time must be in the future.");
            return;
        }

        form.set("publish-date", dateValue);
        form.set("publish-time", timeValue);
    }

    // Validate required fields
    const title = form.get("title")?.toString().trim();
    const description = form.get("description")?.toString().trim();
    const body = form.get("body")?.toString().trim();
    

    if (!title || !description || !body) {
       showToast("error","Title, description, and content are required.");
        return;
    }

    const coverImageInput = document.getElementById("cover-image");
    if (!validateCoverImage(coverImageInput)) {
        showToast("error","Please upload a cover image.");
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
         localStorage.removeItem("smde_post-body-autosave");  
    } catch (error) {
        console.error("Submission failed:", error);
        showToast("error" ,error.message);
    }
});

function validateCoverImage(fileInput) {
  if (!fileInput.files || fileInput.files.length === 0) {
    return false;
  }
  return true;
}
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



//render nav bar correctly
document.addEventListener("DOMContentLoaded", () => {
   const user = getCurrentUser();
  // Show avatar + initials
  
  document.getElementById("user-avatar").textContent = user.initials;
  
   document.getElementById("postManage-link").classList.remove("hidden");
  if(user.role=="Admin")
  document.getElementById("dashboard-link").classList.remove("hidden");
 
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
    tableBody.innerHTML = '<tr><td colspan="6">No posts found</td></tr>';
    return;
  }
  console.log("Raw posts:", posts);
  const sortedPosts = [...posts].sort((a, b) => {
  return new Date(b.createdAt) - new Date(a.createdAt);
  });
  tableBody.innerHTML = sortedPosts.map(post => {
  const statusText = getStatusText(post.status);
  const displayText = statusText.charAt(0).toUpperCase() + statusText.slice(1);
  const canEdit = post.authorId === user.id;

  return `
    <tr class="${isAdmin || post.authorId === user.id ? '' : 'read-only'}">
      <td>${post.title}</td>
      <td>${post.author || 'Unknown'}</td>
      <td>${new Date(post.createdAt).toLocaleDateString()}</td>
      <td>${post.likeCount}</td>
      <td>${post.comments.length}</td>
      <td class="actions">
          <button class="view-btn" data-slug="${post.slug}" title="View Post">
            <i class="fas fa-eye"></i>
          </button>
          <button class="edit-btn" data-slug="${post.slug}" ${!canEdit ? 'disabled' : ''}>
            <i class="fas fa-edit"></i>
          </button>
          <button class="delete-btn" data-id="${post.id}"><i class="fas fa-trash"></i></button>
          <!-- <button class="status-btn" data-id="${post.id}">
            <i class="fas fa-sync-alt"></i> -->
          </button>

      </td>
    </tr>
  `;
}).join('');
;
  
  // Corrected event listener for edit buttons
  document.addEventListener("click", (e) => {
  // Handle edit button clicks (works even when clicking the icon inside)
  const editButton = e.target.closest('.edit-btn');
  if (editButton) {
    const slug = editButton.dataset.slug;
    console.log("Edit button clicked for slug:", slug);
    if (slug) {
      openEditPost(slug);
    } else {
      console.error("No slug found for edit button");
    }
  }
  });
  document.querySelectorAll('.delete-btn').forEach(btn => {
  const postId = btn.dataset.id;
  const post = posts.find(p => p.id === postId); // match the post

  if (post) {
    btn.addEventListener('click', () => openDeleteModal(post));
  }
});

}


//edit event listener
document.addEventListener("click", async (e) => {
  if (e.target.matches(".edit-post-btn")) {
    const slug = e.target.dataset.slug;
    try {
      const res = await fetch(`/api/posts/${slug}`);
      if (!res.ok) throw new Error("Post not found");

      const post = await res.json();
      openEditModal(post); // fills post create with data 
    } catch (err) {
      showToast("Error fetching post: " + err.message); // optional toast
    }
  }
});


//update this to be delete post 
function openDeleteModal(post) {
  const content = document.getElementById("delete-form-content");
  content.innerHTML = `
    <h2>Confirm Deletion</h2>
    <div class="modal-content">
      <p>Are you sure you want to delete <strong>${post.title}</strong>? This action cannot be undone.</p>
      <form id="delete-form">
        <input type="hidden" name="slug" value="${post.slug}" />
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
      const res = await fetch("/api/posts/delete", {
        method: "POST",
        body: formData,
        headers: {
          "Authorization": `Bearer ${localStorage.getItem("authToken")}`
        }
      });

      if (res.ok) {
        
        showToast("success", "Post deleted!");
        document.getElementById("delete-toggle").checked = false;
        loadPostsByStatus(0);
        updatePostLabel("Drafts");
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

//logout handler 
 document.getElementById("logout-btn").addEventListener("click", () => {
    localStorage.clear();
    window.location.href = "/";
  });


// Toast Logic
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



async function openEditPost(slug) {
  try {
    const res = await fetch(`/api/posts/${slug}`);
    if (!res.ok) throw new Error("Post not found");
    const post = await res.json();

    // Switch to "create" section
    navigate("create");

    // Fill basic form fields
    document.getElementById("post-title").value = post.title || "";
    document.getElementById("post-description").value = post.description || "";
    document.getElementById("post-slug").value = post.slug || "";

    if (window.easyMDE) {
      window.easyMDE.value(post.content || "");
    }

    // Cover Image Handling
    if (post.image) {
      const previewImg = document.getElementById("preview-img");
      previewImg.src = post.image;
      previewImg.style.display = "block";
      document.getElementById("placeholder-icon").style.display = "none";
      document.getElementById("upload-message").style.display = "none";
    }
    const oldAttachments = document.querySelector('.existing-attachments');
    if (oldAttachments) oldAttachments.remove();

    // Create a temporary div for attachments (since we don't have attachment-list in HTML)
    let attachmentsHTML = '';
    (post.attachments || []).forEach(file => {
      attachmentsHTML += `
        <div class="existing-attachment">
          <a href="${file.url || file}" target="_blank" class="attachment-link">
            ${file.name || file.split("/").pop()}
          </a>
        </div>
      `;
    });

    // Insert attachments after the file input
    const fileInputGroup = document.querySelector('.form-group input[id="post-files"]').parentNode;
    if (attachmentsHTML) {
      const attachmentsContainer = document.createElement('div');
      attachmentsContainer.className = 'existing-attachments';
      attachmentsContainer.innerHTML = `<label>Existing Attachments</label>${attachmentsHTML}`;
      fileInputGroup.insertAdjacentElement('afterend', attachmentsContainer);
    }

    // Tags and Categories
    await loadTags(); // Make sure tags are loaded first
    await loadCategories(); // Make sure categories are loaded first
    
    // Modified preselectOptions to work with current HTML
    preselectOptions("post-tags", post.tags || []);
    preselectOptions("post-categories", post.categories || []);


    // Handle scheduled posts
    if (post.status === 2 && post.scheduledAt) { // 2 is scheduled
      document.querySelector('input[name="publish-option"][value="schedule"]').checked = true;
      document.getElementById("schedule-container").style.display = "block";
      const date = new Date(post.scheduledAt);
      document.getElementById("publish-date").value = date.toISOString().split('T')[0];
      document.getElementById("publish-time").value = 
        `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`;
    }

    // Store slug for future PUT
    document.getElementById("post-form").dataset.editSlug = slug;
    
    document.getElementById("section-title").textContent = "Edit Post";

    document.getElementById("save-draft").textContent = "Update Draft";
    document.getElementById("publish-post").textContent = "Update and Publish";


  } catch (err) {
    console.error("Error editing post:", err);
    showToast("error", err.message);
  }
}

function preselectOptions(selectId, values) {
  const select = document.getElementById(selectId);
  if (!select) {
    console.error(`Select element with ID ${selectId} not found`);
    return;
  }

  // Convert values to lowercase for case-insensitive comparison
  const lowerValues = values.map(v => v.toLowerCase());
  
  Array.from(select.options).forEach(opt => {
    // Check if option value or text matches any of the values
    const optValue = opt.value.toLowerCase();
    const optText = opt.text.toLowerCase();
    
    opt.selected = lowerValues.includes(optValue) || lowerValues.includes(optText);
  });
}


document.addEventListener('click', function(e) {
  if (e.target.closest('.view-btn')) {
    const slug = e.target.closest('.view-btn').dataset.slug;
    window.location.href = `/${slug}`;
  }
});
let lastAction = null;

// document.getElementById("save-draft").addEventListener("click", () => {
//   lastAction = document.getElementById("save-draft").textContent;
// });

// document.getElementById("publish-post").addEventListener("click", () => {
//   lastAction = document.getElementById("publish-post").textContent;
// });
