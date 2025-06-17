// not used for now 

document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('postForm');
    const statusMessage = document.getElementById('statusMessage');
    const postPreview = document.getElementById('postPreview');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const postData = {
            title: document.getElementById('title').value,
            content: document.getElementById('content').value,
            tags: document.getElementById('tags').value
                .split(',')
                .map(tag => tag.trim())
                .filter(tag => tag),
            publish: document.getElementById('publish').checked
        };

        try {
            statusMessage.textContent = 'Saving post...';
            statusMessage.className = 'mt-3 text-info';

            const response = await fetch('/api/posts', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(postData)
            });

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const post = await response.json();
            statusMessage.textContent = `Post saved successfully! Slug: ${post.Slug}`;
            statusMessage.className = 'mt-3 text-success';
            
            // Clear form
            form.reset();
            
        } catch (error) {
            statusMessage.textContent = `Error: ${error.message}`;
            statusMessage.className = 'mt-3 text-danger';
            console.error('Error saving post:', error);
        }
    });

    // Simple preview functionality
    document.getElementById('content').addEventListener('input', (e) => {
        // Basic markdown to HTML conversion
        const html = e.target.value
            .replace(/^# (.*$)/gm, '<h2>$1</h2>')
            .replace(/^## (.*$)/gm, '<h3>$1</h3>')
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/\n/g, '<br>');
        
        postPreview.innerHTML = html;
    });
});