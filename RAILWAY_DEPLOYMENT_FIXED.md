# Railway.app Deployment - Fixed for 500 Error

## Problem Fixed
The 500 errors were caused by:
1. FrontendServer couldn't find frontend folder path
2. No environment variable support for MongoDB connection
3. Missing PORT environment variable handling

## What Was Changed
1. ✅ Updated FrontendServer to support dynamic port (PORT env var)
2. ✅ Fixed frontend path resolution (tries multiple locations)
3. ✅ Updated AttendanceAPI to read MONGODB_URL and JWT_SECRET from env
4. ✅ Updated Dockerfile to properly organize files
5. ✅ Created railway.toml configuration

## Step 1: Create MongoDB Atlas (Free Database)
1. Go to https://www.mongodb.com/cloud/atlas
2. Create free account
3. Create a free cluster (M0)
4. Create database user with username/password
5. Click "Connect" → "Connection String"
6. Copy connection string like:
   ```
   mongodb+srv://username:password@cluster0.xxxxx.mongodb.net/UniversityAttendanceDB?retryWrites=true&w=majority
   ```

## Step 2: Push Code to GitHub
```powershell
cd c:\Users\IsaMoma\UniversityAttendance

# If not already a git repo
git init
git add .
git commit -m "Prepare for Railway deployment"

# Add remote
git remote add origin https://github.com/yourusername/university-attendance.git
git branch -M main
git push -u origin main
```

## Step 3: Deploy on Railway.app
1. Go to https://railway.app
2. Click "New Project"
3. Select "Deploy from GitHub repo"
4. Authorize GitHub
5. Select your `university-attendance` repository
6. Railway auto-detects Dockerfile ✓
7. Wait for build to complete (5-10 minutes)

## Step 4: Add Environment Variables in Railway
1. Go to your project in Railway dashboard
2. Click "Variables"
3. Add these variables:
   ```
   MONGODB_URL = mongodb+srv://username:password@cluster0.xxxxx.mongodb.net/UniversityAttendanceDB?retryWrites=true&w=majority
   JWT_SECRET = your-super-secret-key-change-this-in-production
   ASPNETCORE_ENVIRONMENT = Production
   PORT = 3000
   ```

## Step 5: Deploy
Railway auto-deploys when you push to GitHub. Check:
1. Build logs in Railway dashboard
2. Deployment status should show "✓ Deployed"
3. Click "View Logs" to see real-time output

## Step 6: Test Your App
1. Railway generates a URL like: `https://university-attendance-production.up.railway.app`
2. Visit the URL - should see your frontend
3. API endpoints at: `/api/...`
4. Swagger UI at: `/swagger`

## If Still Getting 500 Errors

### Check Logs
In Railway dashboard → View Logs:
```
Look for error messages about:
- MongoDB connection failed
- Frontend files not found
- Port binding issues
```

### Common Issues

**1. MongoDB Connection Error**
- Verify MONGODB_URL is correct
- Check MongoDB Atlas whitelist includes Railway IP (usually allows all)
- Ensure database exists in Atlas

**2. Frontend Not Found**
- Check logs for path messages
- Verify `frontend/` folder structure is in repo
- Ensure `index.html` exists in `frontend/` folder

**3. Port Issues**
- Railway provides PORT env var
- App reads it from environment
- Should bind to 0.0.0.0 (not localhost)
✓ Already fixed in code

## File Changes Made

### 1. FrontendServer/Program.cs
- Reads PORT from environment variable
- Binds to 0.0.0.0 (not localhost)
- Tries multiple paths for frontend folder

### 2. AttendanceAPI/Program.cs
- Reads MONGODB_URL from environment
- Reads JWT_SECRET from environment
- Falls back to config values if env vars not set

### 3. appsettings.json
- Updated to support environment variables
- Fallback values for local development

### 4. Dockerfile
- Multi-stage build for optimal size
- Copies both FrontendServer and AttendanceAPI
- Copies frontend static files
- Runs FrontendServer by default (serves both UI and API)

## Deployment Checklist
- [ ] Code pushed to GitHub
- [ ] Railway connected to GitHub repo
- [ ] MongoDB Atlas cluster created
- [ ] MONGODB_URL env var set in Railway
- [ ] JWT_SECRET env var set in Railway
- [ ] Build completed successfully
- [ ] App is running (check logs)
- [ ] Access URL works

## Next: Deploy Backend API Separately (Optional)
If you want both FrontendServer AND AttendanceAPI running:
1. Create second Railway project
2. Deploy AttendanceAPI from same repo
3. In FrontendServer, update API calls to point to the new API URL
4. Use environment variable for API_URL in frontend

Or modify Dockerfile to run both simultaneously using docker-compose inside container.

## Support
- Railway docs: https://docs.railway.app
- MongoDB docs: https://docs.mongodb.com/cloud/atlas
- Check Railway logs for detailed error messages
