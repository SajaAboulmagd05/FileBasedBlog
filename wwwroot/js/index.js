let currentPage = 1;
let currentCategory = null;
let selectedTags = [];

const postList = document.getElementById("post-list");
const categoryFilter = document.getElementById("categories");
const tagFilter = document.getElementById("tags");
const pageInfo = document.getElementById("page-info");

async function loadPosts() {
  try {
    const response = await fetch("/api/posts");
    const data = await response.json();
    renderPosts(data.posts || []);
  } catch (err) {
    document.getElementById("post-list").innerHTML = "<p>Could not load posts.</p>";
    console.error(err);
  }
}

function renderPosts(posts) {
  const postList = document.getElementById("post-list");
  postList.innerHTML = "";

  if (posts.length === 0) {
    postList.innerHTML = "<p>No posts available.</p>";
    return;
  }

  posts.forEach(post => {
    const article = document.createElement("article");

    article.innerHTML = `
      <div class="post-preview">
        ${post.image ? `<img src="${post.image}" alt="cover image" class="post-thumb" />` : ""}
        <div class="post-content">
          <h2><a href="/posts/${post.slug}">${post.title}</a></h2>
          <p>${post.description}</p>
          <small>${new Date(post.createdAt).toLocaleDateString()} | 📎 ${post.attachmentCount} file(s)</small>
        </div>
      </div>
    `;

    postList.appendChild(article);
  });
}

document.addEventListener("DOMContentLoaded", loadPosts);


function updatePagination(totalPages) {
  pageInfo.textContent = `Page ${currentPage} of ${totalPages}`;
  document.getElementById("prev-page").disabled = currentPage === 1;
  document.getElementById("next-page").disabled = currentPage === totalPages;
}

document.getElementById("prev-page").addEventListener("click", () => {
  if (currentPage > 1) {
    currentPage--;
    loadPosts();
  }
});

document.getElementById("next-page").addEventListener("click", () => {
  currentPage++;
  loadPosts();
});

//loading the available categories 
async function loadCategories() {
  const res = await fetch("/api/categories");
  const categories = await res.json();

  categoryFilter.innerHTML = `<button data-category="">All</button>`;
  categories.forEach(cat => {
    const btn = document.createElement("button");
    btn.textContent = cat.Name;
    btn.dataset.category = cat.Name;
    categoryFilter.appendChild(btn);
  });

  categoryFilter.addEventListener("click", e => {
    if (e.target.tagName === "BUTTON") {
      currentCategory = e.target.dataset.category || null;
      currentPage = 1;
      loadPosts();
    }
  });
}


//loading the available tags 
async function loadTags() {
  const res = await fetch("/api/tags");
  const tags = await res.json();

  tags.forEach(tag => {
    const label = document.createElement("label");
    label.innerHTML = `
      <input type="checkbox" value="${tag.Name}" />
      ${tag.Name}
    `;
    tagFilter.appendChild(label);
  });

  tagFilter.addEventListener("change", () => {
    selectedTags = [...tagFilter.querySelectorAll("input:checked")].map(cb => cb.value);
    currentPage = 1;
    loadPosts();
  });
}

document.addEventListener("DOMContentLoaded", () => {
  loadCategories();
  loadTags();
  loadPosts();
});
