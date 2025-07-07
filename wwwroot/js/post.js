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

//   const comments = post.comments.map(comment => `
//     <div class="comment">
//       <div class="comment-meta">
//         <span class="comment-id">#${comment.subscriberID}</span>
//         <span class="comment-date">${new Date(comment.createdAt).toLocaleString()}</span>
//       </div>
//       <p class="comment-text">${comment.content}</p>
//     </div>
//   `).join("");

  const html = post.content ;
  //? marked.parse(post.content) : "<p>No content available.</p>";

  container.innerHTML = `
    <h1 class="post-title">${post.title}</h1>
    <p class="post-description">${post.description}</p>

    <div class="post-meta">
      <span><i class="far fa-clock"></i> ${formattedDate}</span>
      <span>• ${post.readingTime} likes and number of comments should come here </span>
   
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

    <div class="post-body">${html}</div>

   
  `;
}

 //   <span>• ${post.likeCount} ❤️</span>
 // <div class="comments-section">
    //   <h3>Comments (${post.comments.length})</h3>
    //   ${comments || "<p>No comments yet.</p>"}
    // </div>