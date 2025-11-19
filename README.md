# Aetherion MMO Server

Backend server для Aetherion - мультиплеерной MMO RPG игры на Unity.

## 🚀 Tech Stack

- **Node.js** (v18+)
- **Express** - REST API  
- **Socket.IO** - Real-time multiplayer
- **MongoDB** - Database (Mongoose ODM)
- **JWT** - Authentication

## 📁 Project Structure

```
├── server.js              # Main entry point
├── multiplayer.js         # Socket.IO multiplayer logic (2300+ lines)
├── config/
│   └── db.js             # MongoDB connection
├── models/
│   ├── User.js           # User model
│   ├── Character.js      # Character model (5 classes)
│   ├── Room.js           # Room/lobby model
│   └── Party.js          # Party/group model
├── routes/
│   ├── auth.js           # Auth endpoints
│   ├── character.js      # Character management
│   ├── room.js           # Room management
│   └── party.js          # Party management
├── controllers/
│   ├── authController.js
│   └── characterController.js
└── middleware/
    └── auth.js           # JWT middleware
```

## 🎮 Features

### MMO Persistent World
- Global room with up to 500 players
- Real-time position synchronization
- Character animations sync
- Combat system (attacks, skills, damage)

### Inventory System
- 40-slot MMO inventory
- Equipment (weapon, armor, helmet, accessory)
- MongoDB persistence
- Offline fallback (PlayerPrefs)

### Character System
- 5 classes: Warrior, Mage, Archer, Rogue, Paladin
- SPECIAL stats: Strength, Perception, Endurance, Wisdom, Intelligence, Agility, Luck
- Level progression
- One character per class per account

## 🔧 Installation

```bash
# Install dependencies
npm install

# Create .env file with your credentials
# See Environment Variables section below
```

### Environment Variables
```env
NODE_ENV=production
PORT=5000
MONGODB_URI=mongodb+srv://user:pass@cluster.mongodb.net/aetherion
JWT_SECRET=your-secret-key
ALLOWED_ORIGINS=https://yourdomain.com
```

## 🏃 Running

```bash
# Development
npm run dev

# Production
npm start
```

## 🌍 Deployment (Render.com)

**Important:** Deploy from `server-only` branch!

```bash
git push origin server-only
```

Render will auto-detect changes and redeploy.

## 📊 Stats

- Repository Size: 890 MB (cleaned from 9.4 GB)
- Server Code: ~4,600 lines
- Supports: 500+ concurrent players

## 📝 Git Branches

- `main` - Full project (Unity + Server)
- `server-only` - Server code only (for Render)

## 📄 License

MIT
