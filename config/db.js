const mongoose = require('mongoose');

const connectDB = async () => {
  try {
    // Проверяем наличие MONGODB_URI
    if (!process.env.MONGODB_URI) {
      console.error('❌ MONGODB_URI is not defined in environment variables!');
      console.error('📋 Please set MONGODB_URI in Render Dashboard:');
      console.error('   1. Go to your service on Render');
      console.error('   2. Navigate to Environment tab');
      console.error('   3. Add MONGODB_URI variable with your MongoDB connection string');
      console.error('   Example: mongodb+srv://username:password@cluster.mongodb.net/aetherion');
      process.exit(1);
    }

    await mongoose.connect(process.env.MONGODB_URI, {
      useNewUrlParser: true,
      useUnifiedTopology: true,
    });
    console.log('✅ MongoDB Connected successfully');
    console.log(`📊 Database: ${mongoose.connection.name}`);
  } catch (error) {
    console.error('❌ MongoDB Connection Error:', error.message);
    console.error('💡 Check that your MongoDB URI is correct and MongoDB Atlas allows connections from Render IP addresses');
    process.exit(1);
  }
};

module.exports = connectDB;
