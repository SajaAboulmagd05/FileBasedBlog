// Load initials into avatar
document.addEventListener("DOMContentLoaded", () => {
  // Initialize avatar
  document.getElementById("profile-avatar").textContent = localStorage.getItem('userInitials') || "SA";
  
  // Load profile data
  loadProfile();
   // Check user role for admin links
  const user = getCurrentUser();
   if (user.role === "Admin") {
    document.querySelector('.tab-btn[data-tab="role-request"]').style.display = "none";
  }
  
  const dashboardLink = document.getElementById("dashboard-link");
  const PostManagmentLink= document.getElementById("postManage-link");
  const menu = document.getElementById("user-menu");
 

  if (["Admin"].includes(user.role)) {
    dashboardLink.classList.remove("hidden");
  }

  if (["Admin", "Editor", "Author"].includes(user.role)) {
    PostManagmentLink.classList.remove("hidden");
  }
  document.getElementById("user-avatar").textContent = user.initials;
  menu.classList.remove("hidden");
   
  // Initialize tab functionality
  setupTabs();
  
 
});

// Tab functionality
function setupTabs() {
  const tabButtons = document.querySelectorAll(".tab-btn");
  const tabContents = document.querySelectorAll(".tab-content");
  
  tabButtons.forEach(button => {
    button.addEventListener("click", () => {
      // Remove active class from all buttons and contents
      tabButtons.forEach(btn => btn.classList.remove("active"));
      tabContents.forEach(content => content.classList.remove("active"));
      
      // Add active class to clicked button and corresponding content
      button.classList.add("active");
      const tabId = button.getAttribute("data-tab");
      document.getElementById(tabId).classList.add("active");
    });
  });
  
  // Set up form submissions for each tab
  setupFormSubmissions();
}

function setupFormSubmissions() {
  // Change Name Form
  document.querySelector("#change-name form").addEventListener("submit", async (e) => {
    e.preventDefault();
    const newName = e.target.querySelector("input[name='new-name']").value;
    
    try {
      const res = await fetch("/api/users/change-name", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${localStorage.getItem("authToken")}`
        },
        body: JSON.stringify({ name: newName })
      });
      
      if (res.ok) {
        showToast("success", "Name updated successfully!");
        localStorage.setItem('userName', newName);
        document.getElementById("name-display").textContent = newName;
      } else {
        const error = await res.text();
        showToast("error", error);
      }
    } catch (err) {
      showToast("error", "Network error");
    }
  });
  
  // Reset Password Form
  document.querySelector("#reset-password form").addEventListener("submit", async (e) => {
    e.preventDefault();
    const formData = new FormData(e.target);
    const currentPassword = formData.get("current-password");
    const newPassword = formData.get("new-password");
    const confirmPassword = formData.get("confirm-password");
    
    if (newPassword !== confirmPassword) {
      showToast("error", "New passwords don't match");
      return;
    }
    
    try {
      const res = await fetch("/api/users/change-password", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${localStorage.getItem("authToken")}`
        },
        body: JSON.stringify({
          currentPassword,
          newPassword
        })
      });
      
      if (res.ok) {
        showToast("success", "Password changed successfully!");
        e.target.reset();
      } else {
        const error = await res.text();
        showToast("error", error);
      }
    } catch (err) {
      showToast("error", "Network error");
    }
  });
  
  // Delete Account Form
  document.querySelector("#delete-account form").addEventListener("submit", async (e) => {
    e.preventDefault();
    const password = e.target.querySelector("input[name='delete-password']").value;
    
    if (!confirm("Are you sure you want to delete your account? This cannot be undone.")) {
      return;
    }
    
    try {
      const res = await fetch("/api/users/delete-account", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${localStorage.getItem("authToken")}`
        },
        body: JSON.stringify({ password })
      });
      
      if (res.ok) {
        showToast("success", "Account deleted successfully");
        localStorage.clear();
        setTimeout(() => window.location.href = "/", 2000);
      } else {
        const error = await res.text();
        showToast("error", error);
      }
    } catch (err) {
      showToast("error", "Network error");
    }
  });
  
  // Role Request Form
  document.querySelector("#role-request form").addEventListener("submit", async (e) => {
    e.preventDefault();
    const formData = new FormData(e.target);
    const requestedRole = formData.get("requested-role");
    const requestMessage = formData.get("request-message");
    
    try {
      const res = await fetch("/api/users/request-role-change", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${localStorage.getItem("authToken")}`
        },
        body: JSON.stringify({
          requestedRole,
          requestMessage
        })
      });
      
      if (res.ok) {
        showToast("success", "Role change request submitted!");
        e.target.reset();
      } else {
        const error = await res.text();
        showToast("error", error);
      }
    } catch (err) {
      showToast("error", "Network error");
    }
  });
}

// Fetch user profile
async function loadProfile() {
  try {
    const res = await fetch("/api/users/me", {
      headers: { "Authorization": `Bearer ${localStorage.getItem("authToken")}` }
    });

    if (!res.ok) {
      showToast("error", "Failed to load profile");
      return;
    }

    const user = await res.json();
    
    // Update profile display
    document.getElementById("name-display").textContent = user.name;
    document.getElementById("email-display").textContent = user.email;
    document.getElementById("role-display").textContent = user.role;
    document.getElementById("joined-display").textContent = new Date(user.createdDate).toLocaleDateString();
    
    // Set form values
    document.querySelector("#change-name input[name='new-name']").value = user.name;
  } catch (err) {
    showToast("error", "Network error loading profile");
  }
}


// Get current user from localStorage
function getCurrentUser() {
  return {
    token: localStorage.getItem("authToken"),
    id: localStorage.getItem('userId'), 
    role: localStorage.getItem('userRole'),
    name: localStorage.getItem('userName'),
    initials: localStorage.getItem('userInitials')
  };
}

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

//logout handler 
 document.getElementById("logout-btn").addEventListener("click", () => {
    localStorage.clear();
    window.location.href = "/";
  });


//handle the branding menu when hovering on avatar 
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