let currentPage = 1;
const postsPerPage = 2;
let allPosts = [];
let currentCategory = null;
let selectedTags = [];

async function loadPosts() {
  try {
    let url = "/api/posts";

    // Prioritize category filter
    if (currentCategory) {
      url = `/api/posts/by-category?category=${encodeURIComponent(currentCategory)}`;
    } else if (selectedTags.length > 0) {
      url = `/api/posts/by-tags?tags=${encodeURIComponent(selectedTags.join(","))}`;
    }

    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    allPosts = data.posts || [];
    renderPosts(currentPage);
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

  const totalPages = Math.ceil(allPosts.length / postsPerPage);
  currentPage = Math.max(1, Math.min(page, totalPages));

  const start = (currentPage - 1) * postsPerPage;
  const end = start + postsPerPage;
  const paginatedPosts = allPosts.slice(start, end);

  paginatedPosts.forEach(post => {
    const readingTime = post.readingTime || "2 min read";

    // Generate category buttons
    const categoriesHTML = post.categories?.map(cat => `
      <span class="category-btn">${cat}</span>
   `).join("") || "";

    // Generate tag labels
    const tagsHTML = post.tags?.map((tag, index, arr) => `
      <span class="tag-label">${tag}</span>${index < arr.length - 1 ? '<span class="dot-separator">•</span>' : ''}
   `).join("") || "";

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
            <span class="dot-separator">•</span>
            <span>${readingTime}</span>
          </div>
          <div class="post-content">
            <h3 class="title"><a href="/post.html?slug=${post.slug}" class="custom-link">${post.title}</a></h3>
            <p class="text">${post.description || 'No description available'}</p>
            <div class="details">
              <span class="author">
                <i class="far fa-user"></i>
                <span>by admin</span>
              </span>
              <span class="likes">
                <i class="fas fa-thumbs-up"></i>
                <span>${post.likeCount} Likes</span>
              </span>
               <span class="comments">
                <i class="fas fa-comment"></i>
                <span>10 comments</span>
              </span>
              <span class="attachments">
                <i class="fas fa-paperclip"></i>
                <span>${post.attachmentCount || 0} file(s)</span>
              </span>
            </div>
            <div class="meta">
              <div class="meta-row">
                <div class="meta-section categories">
                  <strong>Categories:</strong>
                  <div class="category-list">${categoriesHTML}</div>
                </div>
                <div class="meta-section tags">
                  <strong>Tags:</strong>
                  <div class="tag-list">${tagsHTML}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;

    postList.appendChild(article);
  });

  const pagination = document.createElement("div");
  pagination.className = "pagination";
  pagination.innerHTML = `
    <button class="btn prev-btn" ${currentPage === 1 ? 'disabled' : ''} onclick="renderPosts(${currentPage - 1})">Previous</button>
    <span class="page-info">Page ${currentPage} of ${totalPages}</span>
    <button class="btn next-btn" ${currentPage >= totalPages ? 'disabled' : ''} onclick="renderPosts(${currentPage + 1})">Next</button>
  `;

  postList.appendChild(pagination);
}

async function loadCategories() {
  try {
    const res = await fetch("/api/categories");
    if (!res.ok) throw new Error(`HTTP error! status: ${res.status}`);

    const categories = await res.json();
    const categoryContainer = document.querySelector(".category");

    categoryContainer.innerHTML = `<button class="category-btn" data-category="">All</button>`;

    categories.forEach(cat => {
      const btn = document.createElement("button");
      btn.className = "category-btn";
      btn.textContent = cat.Name;
      btn.dataset.category = cat.Name;
      categoryContainer.appendChild(btn);
    });

    categoryContainer.addEventListener("click", e => {
      if (e.target.tagName === "BUTTON") {
        categoryContainer.querySelectorAll(".category-btn").forEach(btn => btn.classList.remove("active"));
        e.target.classList.add("active");

        currentCategory = e.target.dataset.category || null;
        selectedTags = []; // Clear tag filters when category is selected
        document.querySelectorAll(".tag input[type='checkbox']").forEach(cb => cb.checked = false);

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
    if (!res.ok) throw new Error(`HTTP error! status: ${res.status}`);

    const tags = await res.json();
    const tagContainer = document.querySelector(".tag");
    tagContainer.innerHTML = "";

    tags.forEach(tag => {
      const label = document.createElement("label");
      label.className = "tag-label";
      label.innerHTML = `
        <input type="checkbox" value="${tag.Name}" />
        <span>${tag.Name}</span>
      `;
      tagContainer.appendChild(label);
    });

    const applyButton = document.createElement("button");
    applyButton.id = "apply-tag-filter";
    applyButton.className = "btn apply-tag-filter";
    applyButton.textContent = "Apply Tag Filter";
    tagContainer.appendChild(applyButton);

    applyButton.addEventListener("click", () => {
      selectedTags = [...tagContainer.querySelectorAll("input:checked")].map(cb => cb.value);
      currentCategory = null; // Clear category filter when applying tag filter
      document.querySelectorAll(".category-btn").forEach(btn => btn.classList.remove("active"));
      currentPage = 1;
      loadPosts();
    });
  } catch (err) {
    console.error("Error loading tags:", err);
    document.querySelector(".tag").innerHTML = "<p>Could not load tags.</p>";
  }
}

//function to handel search functionality 
function handleSearch(event) {
  event.preventDefault();

  const input = document.querySelector(".search-box");
  const query = input.value.trim();
  if (!query) return;

  const loading = document.getElementById("loading-indicator");
  const results = document.getElementById("posts-container");

  // Clear everything immediately to simulate a reload
  results.innerHTML = "";
  loading.style.display = "block";

  const startTime = Date.now();

  fetch(`/api/posts/search?query=${encodeURIComponent(query)}`)
    .then(res => res.json())
    .then(data => {
      allPosts = data.posts || [];
      currentCategory = null;
      selectedTags = [];
      currentPage = 1;

      renderPosts(currentPage);

      // Scroll to results
      results.scrollIntoView({ behavior: "smooth" });
    })
    .catch(err => {
      results.innerHTML = "<p>Search failed.</p>";
    })
    .finally(() => {
      const elapsed = Date.now() - startTime;
      const delay = Math.max(400 - elapsed, 0); // ensure at least 400ms

      setTimeout(() => {
        loading.style.display = "none";
      }, delay);
    });
}


// // adding a banner to dispaly category name 
// categoryContainer.addEventListener("click", e => {
//   if (e.target.tagName === "BUTTON") {
//     categoryContainer.querySelectorAll(".category-btn").forEach(btn => btn.classList.remove("active"));
//     e.target.classList.add("active");

//     currentCategory = e.target.dataset.category || null;
//     selectedTags = [];
//     document.querySelectorAll(".tag input[type='checkbox']").forEach(cb => cb.checked = false);

//     currentPage = 1;

//     // Update the banner
//     const banner = document.getElementById("category-banner");
//     if (currentCategory) {
//       banner.textContent = `Showing posts in: ${currentCategory}`;
//       banner.style.display = "block";
//     } else {
//       banner.style.display = "none";
//     }

//     loadPosts();
//   }
// });


document.addEventListener("DOMContentLoaded", () => {
  loadCategories();
  loadTags();
  loadPosts();
  document.querySelector(".search-form").addEventListener("submit", handleSearch);

  // document.querySelector(".search-form").addEventListener("submit", handleSearch);
});
