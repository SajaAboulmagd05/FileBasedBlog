
const token = localStorage.getItem("authToken");
const isLoggedIn = !!token;
const subscriberID = token ? parseToken(token).name : null;

function parseToken(token) {
  try {
    return JSON.parse(atob(token.split('.')[1]));
  } catch {
    return {};
  }
}

document.addEventListener("DOMContentLoaded", async () => {
  let slug = window.location.pathname.split('/').pop();
  if (!slug || slug === "post.html") {
    const params = new URLSearchParams(window.location.search);
    slug = params.get("slug");
  }
  try {
    const res = await fetch(`/api/posts/${slug}`);
    if (!res.ok) {
      const errorText = await res.text();
      throw new Error(errorText);
    }

    const post = await res.json();
    renderPost(post);
  } catch (err) {
    document.getElementById("post-content").innerHTML = "<p>Post not found.</p>";
    console.error(err);
  }
});

function renderPost(post) {
  const container = document.getElementById("post-content");

  const formattedDate = new Date(post.createdAt).toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric"
  });

  const tags = post.tags.map((tag, i, arr) =>
    `<span class="tag-label">${tag}</span>${i < arr.length - 1 ? '<span class="dot-separator">•</span>' : ''}`
  ).join("");

  const categories = post.categories.map(cat =>
    `<span class="category-btn">${cat}</span>`
  ).join("");

  const html = post.content ? marked.parse(post.content) : "<p>No content available.</p>";

  const attachmentHTML = post.attachments?.map(file =>
    `<li><a href="${file}" download class="attachment-link">${file.split('/').pop()}</a></li>`
  ).join("") || "<p>No attachments available.</p>";

  const attachmentsSection = `
    <div class="attachments-section">
      <h4>Attachments (click to download)</h4>
      <ul>${attachmentHTML}</ul>
    </div>
  `;

  // 🔄 Conditional comment section
  let commentsSectionHTML = "";

  if (!isLoggedIn) {
    commentsSectionHTML = `
      <div class="comments-section locked-section">
        <h3>Join the conversation</h3>
        <p class="comment-gate-message">
          Commenting is reserved for registered members.
        </p>
        <label for="toggle" class="subscribe-btn">Register Now</label>
      </div>
    `;
  } else {
    const comments = post.comments?.map(comment => `
      <div class="comment">
        <div class="comment-meta">
          <span class="comment-id">#${comment.subscriberID || "anon"}</span>
          <span class="comment-date">${new Date(comment.createdAt).toLocaleString()}</span>
        </div>
        <p class="comment-text">${comment.content}</p>
      </div>
    `).join("") || "<p>No comments yet.</p>";

    commentsSectionHTML = `
      <div class="comments-section">
        <h3>Comments (${post.comments?.length || 0})</h3>
        <div class="comment-list">${comments}</div>

        <div class="comment-form">
          <h4>Add a Comment</h4>
          <textarea id="comment-input" placeholder="Share your thoughts with us."></textarea>
          <button class="comment-btn">Submit Comment</button>
        </div>
      </div>
    `;
  }

  // 🧩 Full post markup
  container.innerHTML = `
    <h1 class="post-title">${post.title}</h1>
    <p class="post-description">${post.description}</p>

    <div class="post-meta">
      <span><i class="far fa-clock"></i> ${formattedDate}</span>
      <span>• ${post.readingTime}</span>
    </div>

    <div class="details">
      <span class="author"><i class="far fa-user"></i> <span>${post.author || "By Admin"}</span></span>
      <span class="likes"><i class="fas fa-thumbs-up"></i> <span>${post.likeCount || 0} Likes</span></span>
      <span class="comments"><i class="fas fa-comment"></i> <span>${post.comments?.length || 0} Comments</span></span>
      <span class="attachments"><i class="fas fa-paperclip"></i> <span>${post.attachmentCount || 0} file(s)</span></span>
    </div>

    <div class="meta-row">
      <div class="meta-section categories">
        <strong>Categories:</strong>
        <div class="category-list">${categories}</div>
      </div>
      <div class="meta-section tags">
        <strong>Tags:</strong>
        <div class="tag-list">${tags}</div>
      </div>
    </div>

    ${post.image ? `<img src="${post.image}" alt="cover image" class="post-image" />` : ""}

    <div class="post-body">${html}</div>

    ${attachmentsSection}

    ${commentsSectionHTML}
  `;

  // Like button logic
  const likeEl = container.querySelector(".likes");

  if (isLoggedIn && post.likedByUserIds?.includes(subscriberID)) {
    likeEl.classList.add("liked");
  }

  likeEl.addEventListener("click", async () => {
    if (!isLoggedIn) {
      showToast("Please register to like posts.", "error");
      return;
    }

    const res = await fetch(`/api/posts/${post.slug}/like`, {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${localStorage.getItem("authToken")}`
      }
    });

    if (!res.ok) {
      const error = await res.text();
      showToast(error || "Failed to like post.", "error");
      return;
    }

    const result = await res.json();
    likeEl.classList.toggle("liked", result.liked);
    likeEl.querySelector("span").textContent = `${result.likes} Likes`;
  });

  // 💬 Comment submission
  if (isLoggedIn) {
    document.querySelector(".comment-btn").addEventListener("click", async () => {
      const content = document.getElementById("comment-input").value.trim();
      if (!content) return;

      const formData = new FormData();
      formData.append("content", content);

      const res = await fetch(`/api/posts/${post.slug}/comment`, {
        method: "POST",
        headers: {
          "Authorization": `Bearer ${localStorage.getItem("authToken")}`
        },
        body: formData
      });

      const comments = await res.json();
      updateCommentList(comments);
      document.getElementById("comment-input").value = "";
    });
  }

  // ⏬ Scroll helpers
  container.querySelector(".comments").addEventListener("click", () => {
    document.querySelector(".comments-section")?.scrollIntoView({ behavior: "smooth" });
  });

  container.querySelector(".attachments").addEventListener("click", () => {
    document.querySelector(".attachments-section")?.scrollIntoView({ behavior: "smooth" });
  });
}

function updateCommentList(comments) {
  const list = document.querySelector(".comment-list");
  if (!list) return;

  if (!comments.length) {
    list.innerHTML = "<p>No comments yet.</p>";
    return;
  }

  const commentTitle = document.querySelector(".comments-section h3");
  commentTitle.textContent = `Comments (${comments.length})`;

  const metaComments = document.querySelector(".details .comments span");
  if (metaComments) {
  metaComments.textContent = `${comments.length} Comments`;
  }



  list.innerHTML = comments.map(comment => `
    <div class="comment">
      <div class="comment-meta">
        <span class="comment-id">#${comment.subscriberID || "anon"}</span>
        <span class="comment-date">${new Date(comment.createdAt).toLocaleString()}</span>
      </div>
      <p class="comment-text">${comment.content}</p>
    </div>
  `).join("");
}

