# Railway.app Deployment Guide (Recommended - Simpler than Fly.io)

## Why Railway.app?
- ✅ Web-based (no CLI needed)
- ✅ Supports .NET natively
- ✅ GitHub auto-deployment
- ✅ Free tier available ($5 credit/month)
- ✅ Easy environment variables
- ✅ Better support for databases

## Prerequisites
1. GitHub account (free)
2. Push your code to GitHub

## Step-by-Step Deployment

### 1. Push Code to GitHub
```powershell
cd c:\Users\IsaMoma\UniversityAttendance
git init
git add .
git commit -m "Initial commit - University Attendance App"
git branch -M main
git remote add origin https://github.com/yourusername/university-attendance.git
git push -u origin main
```

### 2. Connect to Railway
1. Go to https://railway.app
2. Sign up with GitHub (or email)
3. Authorize GitHub access
4. Click "Create New Project"
5. Select "Deploy from GitHub repo"
6. Choose your repository
7. Railway auto-detects the Dockerfile

### 3. Configure Environment Variables
In Railway dashboard:
1. Go to project settings
2. Add environment variables:
   ```
   MongoDb__ConnectionString=mongodb+srv://user:pass@cluster.mongodb.net/attendance_db
   Jwt__Key=your-secret-key-min-32-chars
   Jwt__Issuer=AttendanceAPI
   Jwt__Audience=AttendanceApp
   Jwt__ExpiresInHours=24
   ASPNETCORE_ENVIRONMENT=Production
   ```

### 4. Setup MongoDB Atlas
1. Go to https://www.mongodb.com/cloud/atlas
2. Create free cluster
3. Create database user
4. Get connection string: `mongodb+srv://user:password@cluster.mongodb.net/database_name`

### 5. Deploy
Railway auto-deploys when you push to GitHub!
- Every push to `main` branch triggers deployment
- View logs in Railway dashboard
- Gets a free domain: `yourproject.up.railway.app`

### 6. Monitor
- View real-time logs
- Check deployment status
- Scale up if needed (pay-as-you-go)

## Costs
- **Free tier**: $5 credit/month (usually enough for small apps)
- **After free tier**: Pay-as-you-go (~$0.50/hour per VM)

## Compare: Railway vs Fly.io vs Azure
| Feature | Railway | Fly.io | Azure |
|---------|---------|--------|-------|
| Setup | Web UI | CLI | Web UI |
| .NET Support | ✅ | ✅ | ✅✅ |
| Free Tier | $5 credit | Shared resources | $200 credit |
| Easiest | ✅ | ⚠️ | ⚠️ |
| CLI Required | ❌ | ✅ | ⚠️ |

## GitHub Push Commands
```powershell
# Initialize repo (if not already done)
git init

# Add all files
git add .

# Commit
git commit -m "Deploy to Railway"

# Add remote
git remote add origin https://github.com/yourusername/university-attendance.git

# Push
git push -u origin main
```

## Next Steps
1. Create GitHub account if you don't have one
2. Push code to GitHub
3. Sign up for Railway.app
4. Connect your GitHub repo
5. Add MongoDB connection string
6. Done! Auto-deployment happens on each push
