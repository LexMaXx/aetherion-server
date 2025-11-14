# Aetherion Server

Node.js + Express + Socket.IO + MongoDB backend for Aetherion MMO RPG.

## Features

- Real-time multiplayer with Socket.IO
- JWT authentication
- MongoDB data persistence
- PvP arena system with rooms
- Party/group system
- Character management with SPECIAL stats
- Inventory system with MongoDB sync
- RESTful API

## Deploy on Render

This server is configured for automatic deployment on Render.

### Environment Variables (Required)

```
MONGODB_URI=mongodb+srv://...
JWT_SECRET=your_secret_here
NODE_ENV=production
```

### Deployment

1. Push to GitHub
2. Render auto-deploys on commit
3. Check logs for successful startup

## Local Development

```bash
npm install
npm run dev
```

Server runs on http://localhost:5000
