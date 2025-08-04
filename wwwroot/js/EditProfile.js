// Load initials into avatar
document.getElementById("profile-avatar").textContent = localStorage.getItem('userInitials') || "SA";

// Fetch user profile
async function loadProfile() {
  const res = await fetch("/api/users/me", {
    headers: { "Authorization": `Bearer ${localStorage.getItem("authToken")}` }
  });

  if (!res.ok) return showToast("error", "Failed to load profile");

  const user = await res.json();
  document.querySelector("input[name='name']").value = user.name;
  document.querySelector("input[name='newsletter']").checked = user.isSubscribedToNewsletter;

  document.getElementById("email-display").textContent = user.email;
  document.getElementById("role-display").textContent = user.role;
  document.getElementById("joined-display").textContent = new Date(user.createdDate).toLocaleDateString();
  document.getElementById("verified-display").textContent = user.isEmailVerified ? "Yes" : "No";
}

loadProfile();

// Submit profile changes
document.getElementById("edit-profile-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const formData = new FormData(e.target);

  try {
    const res = await fetch("/api/users/edit-profile", {
      method: "POST",
      body: formData,
      headers: {
        "Authorization": `Bearer ${localStorage.getItem("authToken")}`
      }
    });

    if (res.ok) {
      showToast("success", "Profile updated!");
    } else {
      const error = await res.text();
      showToast("error", error);
    }
  } catch {
    showToast("error", "Network error");
  }
});

// Open password modal
document.getElementById("change-password-btn").addEventListener("click", () => {
  document.getElementById("password-toggle").checked = true;
  document.getElementById("password-form-content").innerHTML = `
    <h2>Change Password</h2>
    <form id="password-form">
      <label>Current Password<input type="password" name="currentPassword" required /></label>
      <label>New Password<input type="password" name="newPassword" required /></label>
      <label>Confirm Password<input type="password" name="confirmPassword" required /></label>
      <button type="submit" style="background:#f44336;">Update Password</button>
    </form>
  `;

  document.getElementById("password-form").addEventListener("submit", async (e) => {
    e.preventDefault();
    const formData = new FormData(e.target);

    const res = await fetch("/api/users/change-password", {
      method: "POST",
      body: formData,
      headers: {
        "Authorization": `Bearer ${localStorage.getItem("authToken")}`
      }
    });

    if (res.ok) {
      showToast("success", "Password updated!");
      document.getElementById("password-toggle").checked = false;
    } else {
      const error = await res.text();
      showToast("error", error);
    }
  });
});

// Request role change
document.getElementById("request-role-btn").addEventListener("click", async () => {
  const res = await fetch("/api/users/request-role-change", {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${localStorage.getItem("authToken")}`
    }
  });

  if (res.ok) {
    showToast("success", "Role change request sent!");
  } else {
    const error = await res.text();
    showToast("error", error);
  }
});

document.addEventListener("DOMContentLoaded", () => {
   const user = getCurrentUser();
  // Show avatar + initials
  
  document.getElementById("user-avatar").textContent = user.initials;
  
   document.getElementById("postManage-link").classList.remove("hidden");
  if(user.role=="Admin")
  document.getElementById("dashboard-link").classList.remove("hidden");
 
});
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