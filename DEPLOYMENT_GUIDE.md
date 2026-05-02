# Fly.io Deployment Guide

## Prerequisites
1. Fly.io account (free) - https://fly.io
2. Fly CLI installed - https://fly.io/docs/hands-on/install-flyctl/
3. MongoDB Atlas account (free) - https://www.mongodb.com/cloud/atlas
4. Git (optional, for version control)

## Step-by-Step Deployment

### 1. Install Fly CLI
Download and install from: https://fly.io/docs/hands-on/install-flyctl/

### 2. Login to Fly.io
```powershell
flyctl auth login
```
This opens your browser to authenticate.

### 3. Create MongoDB Atlas Cluster (Free)
1. Go to https://www.mongodb.com/cloud/atlas
2. Sign up for free account
3. Create a free cluster
4. Create a database user with username/password
5. Get connection string: `mongodb+srv://username:password@cluster.mongodb.net/attendance_db?retryWrites=true&w=majority`

### 4. Launch on Fly.io
```powershell
cd c:\Users\IsaMoma\UniversityAttendance
flyctl launch
```
- Enter app name (e.g., `university-attendance`)
- Select region: `iad` (US East) or closest to you
- Skip database creation (we're using MongoDB Atlas)

### 5. Set Environment Variables (Secrets)
```powershell
flyctl secrets set MongoDb__ConnectionString="mongodb+srv://username:password@cluster.mongodb.net/attendance_db"
flyctl secrets set Jwt__Key="your-very-secret-key-here-min-32-chars"
```

### 6. Deploy Application
```powershell
flyctl deploy
```
Wait for the deployment to complete (5-10 minutes).

### 7. Check Deployment Status
```powershell
flyctl status
flyctl logs
```

### 8. Open Your App
```powershell
flyctl open
```
Your app is now live at: `https://university-attendance.fly.dev`

## Monitoring & Maintenance

### View Logs
```powershell
flyctl logs
flyctl logs --follow  # Real-time logs
```

### Update Your App
After making code changes:
```powershell
flyctl deploy
```

### Restart Your App
```powershell
flyctl restart
```

### View Dashboard
```powershell
flyctl dashboard
```

## Troubleshooting

### App won't start
Check logs: `flyctl logs`
Ensure MongoDB connection string is correct in secrets

### Slow performance
You're on free tier with shared resources. Upgrade if needed.

### Out of memory
Free tier has 256MB per VM. Monitor with `flyctl logs`

## Free Tier Limits
- 3 shared-cpu-1x 256MB VMs
- 160GB outbound data/month
- 3GB persistent storage
- Should be fine for small-medium deployments

## Cost After Free Tier
- $0/month (free tier)
- $5-50/month (if you upgrade for more resources)
- Pay-as-you-go: ~$0.50/hour per additional VM

## Important Notes
1. Change the JWT secret key to something secure
2. Keep MongoDB Atlas connection string secret (use flyctl secrets)
3. Monitor logs regularly for errors
4. Test thoroughly before production use

## Next Steps
1. Update Jwt__Key with a secure random key
2. Create MongoDB Atlas cluster
3. Run `flyctl launch` and follow the prompts
4. Run `flyctl deploy` to deploy
5. Test the application at the generated URL
