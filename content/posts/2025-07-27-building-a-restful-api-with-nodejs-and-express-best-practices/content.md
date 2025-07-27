# Building a RESTful API with Node.js and Express: Best Practices

![API Development](https://example.com/api-development.jpg)

In today's interconnected world, building robust APIs is a crucial skill for backend developers. This guide walks through professional patterns for creating production-ready REST APIs with Node.js and Express.

## Core Principles of REST

1. **Client-Server Separation**
2. **Stateless Operations**
3. **Cacheable Resources**
4. **Uniform Interface**
5. **Layered System**

## Project Setup

Initialize a new project:

```bash
mkdir node-api
cd node-api
npm init -y
npm install express body-parser cors helmet morgan
```

Basic server structure:

```javascript
const express = require('express');
const app = express();

// Middleware
app.use(express.json());
app.use(require('cors')());
app.use(require('helmet')());

// Routes
app.get('/', (req, res) => {
  res.json({ message: 'API Running' });
});

// Start server
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
});
```

## Route Organization

Best practice folder structure:

```
/src
  /controllers
    userController.js
  /routes
    userRoutes.js
  /models
    User.js
  /middlewares
    auth.js
  /config
    db.js
  server.js
```

Example route definition:

```javascript
// routes/userRoutes.js
const router = require('express').Router();
const {
  getUsers,
  getUser,
  createUser,
  updateUser,
  deleteUser
} = require('../controllers/userController');

router.route('/')
  .get(getUsers)
  .post(createUser);

router.route('/:id')
  .get(getUser)
  .put(updateUser)
  .delete(deleteUser);

module.exports = router;
```

## Error Handling

Centralized error middleware:

```javascript
// middlewares/errorHandler.js
const errorHandler = (err, req, res, next) => {
  const statusCode = err.statusCode || 500;
  
  res.status(statusCode).json({
    success: false,
    error: process.env.NODE_ENV === 'production' 
      ? 'Server Error' 
      : err.message,
    stack: process.env.NODE_ENV === 'development' 
      ? err.stack 
      : undefined
  });
};

module.exports = errorHandler;
```

## Security Practices

Essential security middleware:

```javascript
app.use(require('helmet')()); // Sets various HTTP headers
app.use(require('express-rate-limit')({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 100 // limit each IP to 100 requests per window
}));
app.use(require('hpp')()); // Protect against HTTP Parameter Pollution
```

## Database Integration

Mongoose example with MongoDB:

```javascript
// models/User.js
const mongoose = require('mongoose');

const UserSchema = new mongoose.Schema({
  name: {
    type: String,
    required: [true, 'Please add a name']
  },
  email: {
    type: String,
    required: true,
    unique: true,
    match: [
      /^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$/,
      'Please add a valid email'
    ]
  },
  password: {
    type: String,
    required: true,
    minlength: 6,
    select: false
  },
  createdAt: {
    type: Date,
    default: Date.now
  }
});

module.exports = mongoose.model('User', UserSchema);
```

## API Documentation

Using Swagger/OpenAPI:

```yaml
# swagger.yaml
openapi: 3.0.0
info:
  title: User API
  version: 1.0.0
paths:
  /api/v1/users:
    get:
      summary: Get all users
      responses:
        '200':
          description: A list of users
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/User'
```

## Performance Optimization

Key techniques:

1. **Response Compression**
   ```javascript
   app.use(require('compression')());
   ```

2. **Request Validation**
   ```javascript
   const { check, validationResult } = require('express-validator');
   
   router.post('/', [
     check('name', 'Name is required').not().isEmpty(),
     check('email', 'Please include a valid email').isEmail()
   ], createUser);
   ```

3. **Caching Strategies**
   ```javascript
   const apicache = require('apicache');
   const cache = apicache.middleware;
   
   app.get('/api/products', cache('15 minutes'), getProducts);
   ```

## Testing Your API

Example with Jest and Supertest:

```javascript
const request = require('supertest');
const app = require('../server');

describe('User API', () => {
  it('should create a new user', async () => {
    const res = await request(app)
      .post('/api/users')
      .send({
        name: 'Test User',
        email: 'test@example.com',
        password: 'password123'
      });
    expect(res.statusCode).toEqual(201);
    expect(res.body).toHaveProperty('token');
  });
});
```

## Deployment Considerations

1. **Environment Variables**
   ```bash
   NODE_ENV=production
   PORT=4000
   MONGO_URI=mongodb+srv://user:password@cluster.mongodb.net/db
   JWT_SECRET=your_jwt_secret
   ```

2. **Process Manager**
   ```bash
   npm install pm2 -g
   pm2 start server.js --name api-server
   ```

## Conclusion

Building professional-grade APIs requires attention to:
- Clear route organization
- Comprehensive error handling
- Rigorous security practices
- Consistent documentation
- Performance optimization

> **Pro Tip**: Use Postman or Insomnia to test your API endpoints during development.

For further reading, check out:
- [Express.js Documentation](https://expressjs.com/)
- [REST API Design Guide](https://www.moesif.com/blog/technical/api-design/REST-API-Design-Best-Practices/)
- [Node.js Best Practices](https://github.com/goldbergyoni/nodebestpractices)