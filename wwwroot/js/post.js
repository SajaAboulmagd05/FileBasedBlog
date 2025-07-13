document.addEventListener("DOMContentLoaded", async () => {
  const params = new URLSearchParams(window.location.search);
  const slug = params.get("slug");

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

  const comments = post.comments?.map(comment => `
    <div class="comment">
      <div class="comment-meta">
        <span class="comment-id">#${comment.subscriberID || "anon"}</span>
        <span class="comment-date">${new Date(comment.createdAt).toLocaleString()}</span>
      </div>
      <p class="comment-text">${comment.content}</p>
    </div>
  `).join("") || "<p>No comments yet.</p>";

  const attachmentHTML = post.attachments?.map(file =>
    `<li><a href="${file}" download class="attachment-link">${file.split('/').pop()}</a></li>`
  ).join("") || "<p>No attachments available.</p>";

  const attachmentsSection = `
    <div class="attachments-section">
      <h4>Attachments (click to download)</h4>
      <ul>${attachmentHTML}</ul>
    </div>
  `;

  container.innerHTML = `
    <h1 class="post-title">${post.title}</h1>
    <p class="post-description">${post.description}</p>

    <div class="post-meta">
      <span><i class="far fa-clock"></i> ${formattedDate}</span>
      <span>• ${post.readingTime}</span>
    </div>

    <div class="details">
      <span class="author"><i class="far fa-user"></i> <span>by admin</span></span>
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

    <div class="comments-section">
      <h3>Comments (${post.comments?.length || 0})</h3>
      <div class="comment-list">${comments}</div>

      <div class="comment-form">
        <h4>Add a Comment</h4>
        <textarea id="comment-input" placeholder="Commenting is limited to subscribers. You’ll be able to post soon."></textarea>
        <button class="btn submit-btn">Submit Comment</button>
      </div>
    </div>
  `;
}
