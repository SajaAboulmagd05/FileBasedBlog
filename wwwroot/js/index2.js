
async function loadPosts() {
  try {
    const response = await fetch("/api/posts");
    const data = await response.json();
    renderPosts(data.posts || []);
  } catch (err) {
    document.getElementById("posts-container").innerHTML = "<p>Could not load posts.</p>";
    console.error(err);
  }
}

function renderPosts(posts) {
  const postList = document.getElementById("posts-container");
  postList.innerHTML = "";

  if (posts.length === 0) {
    postList.innerHTML = "<p>No posts available.</p>";
    return;
  }

  posts.forEach(post => {
    const article = document.createElement("article");
    const formattedDate = new Date(post.createdAt).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
    });
    article.innerHTML = `
    <div class="post-preview">
        ${post.image ? `<img src="${post.image}" alt="cover image" class="post-thumb" />` : ""}
        <div class="date">
            <i class="far fa-clock"></i>
            <span>${formattedDate}</span>
        </div>
        <div class="post-content">
          <h3 class="title"><a href="/posts/${post.slug}">${post.title}</a></h3>
          <p class="text">${post.description}</p>
                <a href="#" class="user">
                    <i class="far fa-user"></i>
                    <span>by admin</span>
                </a>
                 <a href="#" class="user">
                    <i class="fas fa-paperclip"></i>
                    <span> ${post.attachmentCount} file(s)</span>
                </a>
        </div>
    </div>
    `;

    postList.appendChild(article);
  });
}

document.addEventListener("DOMContentLoaded", loadPosts);