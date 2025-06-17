//left not used for now 

document.addEventListener('DOMContentLoaded', async () => {
    // DOM Elements
    const postsGrid = document.getElementById('postsGrid');
    const tagFilter = document.getElementById('tagFilter');
    
    // Load all posts
    async function loadPosts(filterTag = 'all') {
        try {
            postsGrid.innerHTML = '<div class="loading">Loading posts...</div>';
            
            const response = await fetch('/api/posts');
            if (!response.ok) throw new Error('Failed to fetch posts');
            
            const postFolders = await response.json();
            
            if (postFolders.length === 0) {
                postsGrid.innerHTML = '<div class="no-posts">No posts yet. Check back later!</div>';
                return;
            }
            
            postsGrid.innerHTML = '';
            
            // Load each post's details
            for (const folder of postFolders) {
                const slug = folder.split('-').slice(3).join('-');
                const postResponse = await fetch(`/api/posts/${slug}`);
                
                if (postResponse.ok) {
                    const post = await postResponse.json();
                    renderPostCard(post.meta, post.content);
                }
            }
            
            // Update active tag filter
            document.querySelectorAll('.tag-pill').forEach(pill => {
                pill.classList.toggle('active', 
                    pill.dataset.tag === filterTag || 
                    (filterTag === 'all' && pill.dataset.tag === 'all'));
            });
            
        } catch (error) {
            console.error('Error loading posts:', error);
            postsGrid.innerHTML = `
                <div class="error">
                    <i class="fas fa-exclamation-triangle"></i>
                    Failed to load posts. Please try again later.
                </div>
            `;
        }
    }
    
    // Render a post card
    function renderPostCard(meta, content) {
        const card = document.createElement('div');
        card.className = 'post-card';
        card.innerHTML = `
            <img src="/images/placeholder.jpg" alt="${meta.title}">
            <div class="post-content">
                <h3>${meta.title}</h3>
                <div class="post-meta">
                    <span>${new Date(meta.date).toLocaleDateString()}</span>
                    <span>${Math.max(1, Math.floor(content.length / 1000))} min read</span>
                </div>
                <p>${content.substring(0, 100)}...</p>
                <div class="post-tags">
                    ${meta.tags.map(tag => `
                        <span class="tag-pill" data-tag="${tag}">${tag}</span>
                    `).join('')}
                </div>
                <a href="/post.html?slug=${meta.slug}" class="btn btn-outline">
                    Read More <i class="fas fa-arrow-right"></i>
                </a>
            </div>
        `;
        postsGrid.appendChild(card);
    }
    
    // Initialize tag filtering
    function initTagFilter() {
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('tag-pill')) {
                const tag = e.target.dataset.tag;
                loadPosts(tag);
            }
        });
    }
    
    // Initial load
    loadPosts();
    initTagFilter();
});