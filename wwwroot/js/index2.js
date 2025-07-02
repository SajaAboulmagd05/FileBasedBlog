let currentPage = 1; // Track current page
const postsPerPage = 1; // One post per page
let allPosts = []; // Store all posts globally
let currentCategory = null;
let selectedTags = [];

async function loadPosts() {
  try {
    const response = await fetch("/api/posts");
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    const data = await response.json();
    allPosts = data.posts || []; // Store posts globally
    renderPosts(currentPage); // Render the first page
  } catch (err) {
    const postList = document.getElementById("posts-container");
    postList.innerHTML = "<p>Could not load posts.</p>";
    console.error("Error loading posts:", err);
  }
}

function renderPosts(page) {
  const postList = document.getElementById("posts-container");
  postList.innerHTML = "";

  if (allPosts.length === 0) {
    postList.innerHTML = "<p>No posts available.</p>";
    return;
  }

  // Ensure page is within bounds
  currentPage = Math.max(1, Math.min(page, allPosts.length));
  const start = (currentPage - 1) * postsPerPage;
  const end = start + postsPerPage;
  const paginatedPosts = allPosts.slice(start, end);

  // Render the current post
  const post = paginatedPosts[0]; // Only one post per page
  const article = document.createElement("article");
  const formattedDate = new Date(post.createdAt).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  });

  article.innerHTML = `
    <div class="post-preview">
      <div class="post">
        ${post.image ? `<img src="${post.image}" alt="cover image" class="image" />` : ""}
        <div class="date">
          <i class="far fa-clock"></i>
          <span>${formattedDate}</span>
        </div>
        <div class="post-content">
          <h3 class="title"><a href="/posts/${post.slug}" class="custom-link">${post.title}</a></h3>
          <p class="text">${post.description || 'No description available'}</p>
          <div class="details">
            <span class="author">
              <i class="far fa-user"></i>
              <span>by admin</span>
            </span>
            <span class="attachments">
              <i class="fas fa-paperclip"></i>
              <span>${post.attachmentCount || 0} file(s)</span>
            </span>
          </div>
        </div>
      </div>
    </div>
    <div class="pagination">
      <button class="btn prev-btn" ${currentPage === 1 ? 'disabled' : ''} onclick="renderPosts(${currentPage - 1})">Previous</button>
      <span class="page-info">Post ${currentPage} of ${allPosts.length}</span>
      <button class="btn next-btn" ${currentPage === allPosts.length ? 'disabled' : ''} onclick="renderPosts(${currentPage + 1})">Next</button>
    </div>
  `;

  postList.appendChild(article);
}

async function loadCategories() {
  try {
    const res = await fetch("/api/categories");
    if (!res.ok) {
      throw new Error(`HTTP error! status: ${res.status}`);
    }
    const categories = await res.json();
    const categoryContainer = document.querySelector(".category");

    // Clear existing content
    categoryContainer.innerHTML = `<button class="category-btn" data-category="">All</button>`;
    
    // Add category buttons
    categories.forEach(cat => {
      const btn = document.createElement("button");
      btn.className = "category-btn";
      btn.textContent = cat.Name;
      btn.dataset.category = cat.Name;
      categoryContainer.appendChild(btn);
    });

    // Event listener for category buttons
    categoryContainer.addEventListener("click", e => {
      if (e.target.tagName === "BUTTON") {
        // Update active state
        categoryContainer.querySelectorAll(".category-btn").forEach(btn => {
          btn.classList.remove("active");
        });
        e.target.classList.add("active");

        currentCategory = e.target.dataset.category || null;
        currentPage = 1;
        loadPosts();
      }
    });
  } catch (err) {
    console.error("Error loading categories:", err);
    document.querySelector(".category").innerHTML = "<p>Could not load categories.</p>";
  }
}

async function loadTags() {
  try {
    const res = await fetch("/api/tags");
    if (!res.ok) {
      throw new Error(`HTTP error! status: ${res.status}`);
    }
    const tags = await res.json();
    const tagContainer = document.querySelector(".tag");

    // Clear existing content
    tagContainer.innerHTML = "";

    // Add tag checkboxes
    tags.forEach(tag => {
      const label = document.createElement("label");
      label.className = "tag-label";
      label.innerHTML = `
        <input type="checkbox" value="${tag.Name}" />
        <span>${tag.Name}</span>
      `;
      tagContainer.appendChild(label);
    });

    // Event listener for tag checkboxes
    tagContainer.addEventListener("change", () => {
      selectedTags = [...tagContainer.querySelectorAll("input:checked")].map(cb => cb.value);
      currentPage = 1;
      loadPosts();
    });
  } catch (err) {
    console.error("Error loading tags:", err);
    document.querySelector(".tag").innerHTML = "<p>Could not load tags.</p>";
  }
}

document.addEventListener("DOMContentLoaded", () => {
  loadCategories();
  loadTags();
  loadPosts();
});