
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

  // Generate category labels
     const categories = post.categories?.map((tag, index, arr) => `
      <span class="category-label">${tag}</span>${index < arr.length - 1 ? '<span class="dot-separator">•</span>' : ''}
   `).join("") || "";

  const html = post.content ? marked.parse(post.content) : "<p>No content available.</p>";

  const attachmentHTML = post.attachments?.map(file =>
    `<li><a href="${file}" download class="attachment-link">${file.split('/').pop()}</a></li>`
  ).join("") || "<p>No attachments available.</p>";

  const attachmentsSection = `
    <div class="attachments-section">
      <h3>Attachments (click to download)</h3>
      <ul>${attachmentHTML}</ul>
    </div>
  `;

  // Conditional comment section
  let commentsSectionHTML = "";

  if (!isLoggedIn) {
    commentsSectionHTML = `
      <div class="comments-section locked-section">
        <h3>Join the conversation</h3>
        <p class="comment-gate-message">
          Commenting is reserved for registered members.
        </p>
        <label for="register-now-toggle" class="subscribe-btn" id="register-now-btn">Register Now</label>
      </div>
    `;
  } else {
    const comments = post.comments?.map(comment => `
      <div class="comment diff">
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
          <label for="comment-input" class="visually-hidden">Comment</label>
          <textarea id="comment-input" placeholder="Share your thoughts with us."></textarea>
          <button class="comment-btn">Submit Comment</button>
        </div>
      </div>
    `;
  }

  //  Full post markup
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

    ${post.image ? `<img src="${post.image}" alt="${post.title}" class="post-image" />` : ""}

    <div class="post-body">${html}</div>

    ${attachmentsSection}

    ${commentsSectionHTML}
  `;
  Prism.highlightAll();
  // Like button logic
  const likeEl = container.querySelector(".likes");

  if (isLoggedIn && post.likedByUserIds?.includes(subscriberID)) {
    likeEl.classList.add("liked");
  }

  likeEl.addEventListener("click", async () => {
    if (!isLoggedIn) {
      showToast("error","Please register to like posts.");
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

  // Comment submission
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

  // Scroll helpers
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
    <div class="comment diff">
      <div class="comment-meta">
        <span class="comment-id">#${comment.subscriberID || "anon"}</span>
        <span class="comment-date">${new Date(comment.createdAt).toLocaleString()}</span>
      </div>
      <p class="comment-text">${comment.content}</p>
    </div>
  `).join("");
}

//toggling on showing passwords for login and signup 
document.addEventListener("DOMContentLoaded", () => {
const passwordInput = document.querySelector('input[name="password"]');
const confirmInput = document.getElementById("confirm-password");
const toggle = document.getElementById("show-password");

if (toggle) {
    toggle.addEventListener("change", () => {
        const type = toggle.checked ? "text" : "password";
        passwordInput.type = type;
        confirmInput.type = type;
    });
}
});

document.addEventListener("DOMContentLoaded", () => {
    const loginPasswordInput = document.getElementById("login-password");
    const showLoginPassword = document.getElementById("show-login-password");
    
    if (showLoginPassword) {
        showLoginPassword.addEventListener("change", () => {
            loginPasswordInput.type = showLoginPassword.checked ? "text" : "password";
        });
    }
});


//switching between register and login form 
document.addEventListener("DOMContentLoaded", () => {
  const registerSection = document.getElementById("register-section");
  const loginSection = document.getElementById("login-section");
  const switchToLogin = document.getElementById("switch-to-login");
  const switchToRegister = document.getElementById("switch-to-register");
  const LoginButton = document.getElementById("login-btn");
  const RegisterButton = document.getElementById("register-btn");

  switchToLogin?.addEventListener("click", () => {
    registerSection.classList.add("hidden");
    loginSection.classList.remove("hidden");
  });
  
  LoginButton?.addEventListener("click", () => {
    registerSection.classList.add("hidden");
    loginSection.classList.remove("hidden");
  });
  

  switchToRegister?.addEventListener("click", () => {
    loginSection.classList.add("hidden");
    registerSection.classList.remove("hidden");
  });

  RegisterButton?.addEventListener("click", () => {
    loginSection.classList.add("hidden");
    registerSection.classList.remove("hidden");
  });
});




//register handler 
document.addEventListener("DOMContentLoaded", () => {
  const form = document.getElementById("subscribe-form");

  form?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const formData = new FormData(form);

    try {
      const res = await fetch("/api/register", {
        method: "POST",
        body: formData
      });

      if (res.ok) {
        const toast = document.getElementById("success-toast");
        toast.classList.remove("hidden");
        toast.classList.add("visible");

        setTimeout(() => {
          toast.classList.remove("visible");
          toast.classList.add("hidden");
        }, 4000);

        form.reset();
        document.getElementById("toggle").checked = false;
      } else {
        const errorText = await res.text();
        const errorToast = document.getElementById("error-toast");
        errorToast.querySelector(".message").textContent = errorText;

        errorToast.classList.remove("hidden");
        errorToast.classList.add("visible");

        setTimeout(() => {
          errorToast.classList.remove("visible");
          errorToast.classList.add("hidden");
        }, 4000);
      }
    } catch (err) {
      alert("Something went wrong. Please try again.");
      console.error(err);
    }
  });
});


document.addEventListener("DOMContentLoaded", () => {
  const params = new URLSearchParams(window.location.search);

  if (params.get("verified") === "true") {
    const toast = document.getElementById("success-toast");
    toast.querySelector(".message").textContent = "Email verified! You can now log in 🎉";
    toast.classList.remove("hidden");
    toast.classList.add("visible");

    setTimeout(() => {
      toast.classList.remove("visible");
      toast.classList.add("hidden");
    }, 4000);
  }

  if (params.get("showLogin") === "true") {
    const toggle = document.getElementById("toggle");
    const registerSection = document.getElementById("register-section");
    const loginSection = document.getElementById("login-section");

    if (toggle) toggle.checked = true; // open modal
    if (registerSection && loginSection) {
      registerSection.classList.add("hidden");
      loginSection.classList.remove("hidden");
    }
  }

  window.history.replaceState({}, document.title, window.location.pathname);
});

document.getElementById("login-form")?.addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.target;
  const formData = new FormData(form);

  try {
    const res = await fetch("/api/login", {
      method: "POST",
      body: formData
    });

    if (res.ok) {
      const user = await res.json();

      // Store token
      localStorage.setItem("authToken", user.token);
      localStorage.setItem("userName", user.name);
      localStorage.setItem("userRole", user.role);
      localStorage.setItem("userInitials", user.initials);
      localStorage.setItem("userId", user.id);

      

      // Show welcome toast
      const toast = document.getElementById("success-toast");
      toast.querySelector(".message").textContent = `Welcome, ${user.name}! 🎉`;
      toast.classList.remove("hidden");
      toast.classList.add("visible");

      setTimeout(() => {
        toast.classList.remove("visible");
        toast.classList.add("hidden");
        window.location.reload();
      }, 4000);

      // Show avatar + menu
      showUserMenu(user);
      form.reset();
      document.getElementById("toggle").checked = false;
    } else {
      const errorText = await res.text();
      const errorToast = document.getElementById("error-toast");
      errorToast.querySelector(".message").textContent = errorText;

      errorToast.classList.remove("hidden");
      errorToast.classList.add("visible");

      setTimeout(() => {
        errorToast.classList.remove("visible");
        errorToast.classList.add("hidden");
      }, 4000);
    }
  } catch (err) {
    alert("Login failed. Try again.");
    console.error(err);
  }
});

function showUserMenu(user) {
  const avatar = document.getElementById("user-avatar");
  const menu = document.getElementById("user-menu");
  const dropdown = document.getElementById("user-dropdown");
  const dashboardLink = document.getElementById("dashboard-link");
  const PostManagmentLink= document.getElementById("postManage-link");
  const RegisterButton = document.getElementById("register-btn");
  const NavMenu = document.getElementById("main-nav");
  const LoginButton =document.getElementById("login-btn");

  avatar.textContent = user.initials;
  menu.classList.remove("hidden");
  RegisterButton.classList.add("hidden");
  LoginButton.classList.add("hidden");
  NavMenu.classList.remove("hidden");

  if (["Admin"].includes(user.role)) {
    dashboardLink.classList.remove("hidden");
  }

  if (["Admin", "Editor", "Author"].includes(user.role)) {
    PostManagmentLink.classList.remove("hidden");

  }

  avatar.addEventListener("click", () => {
    dropdown.classList.toggle("hidden");
  });

  
  document.getElementById("logout-btn").addEventListener("click", () => {
    localStorage.clear();
    menu.classList.add("hidden");
    dropdown.classList.add("hidden");
    NavMenu.classList.add("hidden");
    RegisterButton.classList.remove("hidden");
    LoginButton.classList.remove("hidden");
    window.location.href = "/";
  });
}
document.addEventListener("DOMContentLoaded", () => {
  const token = localStorage.getItem("authToken");
  const name = localStorage.getItem("userName");
  const role = localStorage.getItem("userRole");
  const initials = localStorage.getItem("userInitials");

  if (token && name && role && initials) {
    showUserMenu({ name, role, initials });
  }
});

function clearForms() {
  // Clear login form
  const loginForm = document.getElementById('login-form');
  if (loginForm) loginForm.reset();
  
  // Clear registration form
  const registerForm = document.getElementById('subscribe-form');
  if (registerForm) registerForm.reset();
  
  // Reset password fields
  document.querySelector('input[name="password"]').type = 'password';
  document.getElementById('confirm-password').type = 'password';
  document.getElementById('login-password').type = 'password';
  
  // Uncheck "show password" boxes
  document.getElementById('show-password').checked = false;
  document.getElementById('show-login-password').checked = false;
}

function closeModals() {
  // Uncheck both toggle checkboxes
  document.getElementById('login-toggle').checked = false;
  document.getElementById('register-toggle').checked = false;
  document.getElementById('register-now-toggle').checked = false;
  // Clear the forms (your existing function)
  clearForms();
}


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

