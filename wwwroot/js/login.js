
document.getElementById('loginForm').addEventListener('submit', async function(e) {
  e.preventDefault();

  const username = document.getElementById('username').value.trim();
  const password = document.getElementById('password').value;

  try {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });

    if (!response.ok) {
      throw new Error('Invalid credentials');
    }

    const data = await response.json();

    // Store JWT and role (optional)
    localStorage.setItem('jwtToken', data.token);
    localStorage.setItem('userRole', data.role);

    window.location.href = '/CreatePost.html'; 
  } catch (err) {
    alert(err.message);
  }
});
