## 🚀 Why CSS Grid?

CSS Grid is a powerful 2-dimensional layout system built into modern browsers. It lets you build complex layouts with simple declarative rules.

Unlike Flexbox, which excels at 1D layouts (either row *or* column), Grid gives you precise control over both axes at once.

---

## 📐 Basic Example

```css
.grid-container {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
}