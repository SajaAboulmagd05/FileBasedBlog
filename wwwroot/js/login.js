// login.js - Handle form submission
document.getElementById('loginForm').addEventListener('submit', async function(e) {
    e.preventDefault();
    
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    if (username=="admin1" && password=="admino123")
    {
        window.location.href = 'CreatePost'; 
    }
 // for now I can't understand the authentication process and the login api is not done 
    // try {
    //     const response = await fetch('/api/auth/login', {
    //         method: 'POST',
    //         headers: {
    //             'Content-Type': 'application/json'
    //         },
    //         body: JSON.stringify({ username, password })
    //     });

    //     if (!response.ok) {
    //         throw new Error('Login failed');
    //     }

    //     const data = await response.json();
        
    //     // Store the JWT token
    //     localStorage.setItem('jwtToken', data.token);
    //     localStorage.setItem('userRole', data.role);
        
    //     // Redirect to home page
    //     window.location.href = '/Home/Index';
        
    // } catch (error) {
    //     document.getElementById('errorMessage').textContent = error.message;
    // }
});