# Modern CSS Layouts: Flexbox, Grid, and Custom Properties in 2024

![CSS Layout Examples](https://example.com/css-layouts.jpg)

Gone are the days of float-based layouts and pixel-perfect hacks. Modern CSS provides powerful tools for creating flexible, responsive layouts with minimal code. Let's explore the current state of CSS layout techniques.

## CSS Custom Properties (Variables)

### Basic Implementation
```css
:root {
  --primary-color: #3a86ff;
  --secondary-color: #8338ec;
  --spacing-unit: 1rem;
  --max-width: 1200px;
}

.header {
  background-color: var(--primary-color);
  padding: var(--spacing-unit);
  max-width: var(--max-width);
}
```

### Dynamic Theming
```javascript
// Change theme via JS
document.documentElement.style.setProperty('--primary-color', '#ff006e');
```

## Flexbox: The One-Dimensional Workhorse

### Basic Flex Container
```css
.container {
  display: flex;
  gap: 1rem;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
}
```

### Common Use Cases

1. **Navigation Bars**
```css
.nav {
  display: flex;
  gap: 2rem;
}

@media (max-width: 768px) {
  .nav {
    flex-direction: column;
  }
}
```

2. **Card Layouts**
```css
.card-container {
  display: flex;
  flex-wrap: wrap;
  gap: 1.5rem;
}

.card {
  flex: 1 1 300px; /* Grow | Shrink | Basis */
}
```

## CSS Grid: Two-Dimensional Power

### Basic Grid Setup
```css
.layout {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 2rem;
}
```

### Advanced Grid Patterns

1. **Holy Grail Layout**
```css
.holy-grail {
  display: grid;
  grid-template:
    "header header header" auto
    "nav main aside" 1fr
    "footer footer footer" auto
    / 200px 1fr 200px;
}

@media (max-width: 768px) {
  .holy-grail {
    grid-template:
      "header" auto
      "nav" auto
      "main" 1fr
      "aside" auto
      "footer" auto
      / 1fr;
  }
}
```

2. **Masonry Layout**
```css
.masonry {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  grid-auto-rows: masonry; /* New in 2023! */
  gap: 1rem;
}
```

## Combining Flexbox and Grid

### Card Component Example
```css
.card {
  display: flex;
  flex-direction: column;
}

.card-container {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}
```

## Modern CSS Features

### Container Queries
```css
.component {
  container-type: inline-size;
}

@container (min-width: 400px) {
  .component {
    display: flex;
  }
}
```

### :has() Selector
```css
/* Style the parent when it contains an image */
.card:has(img) {
  border: 2px solid var(--primary-color);
}

/* Style form group that contains an invalid input */
.form-group:has(:invalid) {
  background-color: #fff0f0;
}
```

### Scroll Snap
```css
.gallery {
  scroll-snap-type: x mandatory;
  overflow-x: auto;
  display: flex;
}

.gallery img {
  scroll-snap-align: start;
  flex: 0 0 80%;
}
```

## Performance Considerations

1. **Use modern selectors efficiently**
2. **Limit complex calculations in animations**
3. **Utilize CSS containment**
```css
.widget {
  contain: layout paint style;
}
```

4. **Prefer transforms over layout properties for animations**

## Browser Support Checklist

| Feature          | Support |
|------------------|---------|
| Flexbox          | 98.5%   |
| CSS Grid         | 97%     |
| CSS Variables    | 96%     |
| :has()           | 92%     |
| Container Queries| 88%     |

> **Pro Tip**: Always check [Can I Use](https://caniuse.com/) for current browser support before implementing new features in production.

## Practical Exercise: Build a Responsive Dashboard

```css
.dashboard {
  --sidebar-width: 250px;
  
  display: grid;
  grid-template:
    "sidebar main" 1fr
    / var(--sidebar-width) 1fr;
  min-height: 100vh;
}

.sidebar { grid-area: sidebar; }
.main { grid-area: main; }

@media (max-width: 768px) {
  .dashboard {
    grid-template:
      "sidebar" auto
      "main" 1fr
      / 1fr;
  }
}

.widget-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1rem;
  padding: 1rem;
}
```

## Resources for Further Learning

1. [CSS Tricks Complete Guide to Flexbox](https://css-tricks.com/snippets/css/a-guide-to-flexbox/)
2. [Grid by Example](https://gridbyexample.com/)
3. [MDN CSS Custom Properties](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)
4. [Modern CSS Solutions](https://moderncss.dev/)