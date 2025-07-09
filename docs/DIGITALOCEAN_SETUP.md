# DigitalOcean SFTP Storage Setup Guide

This guide will help you set up SFTP storage for Normaize using DigitalOcean droplets.

## Prerequisites

- DigitalOcean account
- Basic knowledge of SSH and Linux commands
- Domain name (optional, for easier access)

## Step 1: Create a DigitalOcean Droplet

1. **Log into DigitalOcean** and click "Create" → "Droplets"

2. **Choose Configuration:**
   - **Distribution**: Ubuntu 22.04 LTS
   - **Plan**: Basic
   - **Size**: $6/month (1GB RAM, 1 CPU, 25GB SSD) - sufficient for file storage
   - **Datacenter**: Choose closest to your users
   - **Authentication**: SSH Key (recommended) or Password
   - **Hostname**: `normaize-sftp` (or your preferred name)

3. **Click "Create Droplet"**

## Step 2: Configure the Droplet

### Connect to your droplet:
```bash
ssh root@your-droplet-ip
```

### Update the system:
```bash
apt update && apt upgrade -y
```

### Install SFTP server:
```bash
apt install openssh-server -y
```

### Create SFTP user:
```bash
# Create user for SFTP
adduser normaize-sftp

# Set password (you'll need this for configuration)
passwd normaize-sftp
```

### Configure SSH for SFTP-only access:
```bash
# Edit SSH config
nano /etc/ssh/sshd_config
```

Add these lines at the end:
```
# SFTP Configuration
Match User normaize-sftp
    ChrootDirectory /home/normaize-sftp
    ForceCommand internal-sftp
    AllowTcpForwarding no
    X11Forwarding no
    PasswordAuthentication yes
```

### Create SFTP directory structure:
```bash
# Create uploads directory
mkdir -p /home/normaize-sftp/uploads

# Set ownership
chown normaize-sftp:normaize-sftp /home/normaize-sftp/uploads

# Set permissions
chmod 755 /home/normaize-sftp/uploads
```

### Restart SSH service:
```bash
systemctl restart ssh
```

## Step 3: Configure Firewall

### Enable UFW (Uncomplicated Firewall):
```bash
ufw enable
ufw allow ssh
ufw allow 22
ufw status
```

## Step 4: Test SFTP Connection

From your local machine:
```bash
sftp normaize-sftp@your-droplet-ip
```

You should be able to connect and navigate to the uploads directory.

## Step 5: Configure Normaize for SFTP

### Environment Variables

Add these to your `.env` file for local development:
```env
STORAGE_PROVIDER=sftp
SFTP_HOST=your-droplet-ip
SFTP_USERNAME=normaize-sftp
SFTP_PASSWORD=your-sftp-password
SFTP_BASEPATH=/uploads
```

### Railway Environment Variables

Add these to your Railway project:
```env
STORAGE_PROVIDER=sftp
SFTP_HOST=your-droplet-ip
SFTP_USERNAME=normaize-sftp
SFTP_PASSWORD=your-sftp-password
SFTP_BASEPATH=/uploads
```

## Step 6: Security Considerations

### Use SSH Keys (Recommended)

1. **Generate SSH key pair** (if you don't have one):
```bash
ssh-keygen -t rsa -b 4096 -C "normaize-sftp"
```

2. **Copy public key to droplet**:
```bash
ssh-copy-id normaize-sftp@your-droplet-ip
```

3. **Disable password authentication**:
```bash
# Edit SSH config
nano /etc/ssh/sshd_config
```

Change:
```
PasswordAuthentication no
```

4. **Restart SSH**:
```bash
systemctl restart ssh
```

### Set up Fail2ban (Optional but Recommended)

```bash
apt install fail2ban -y
systemctl enable fail2ban
systemctl start fail2ban
```

## Step 7: Monitoring and Maintenance

### Check disk usage:
```bash
df -h
```

### Monitor SFTP connections:
```bash
tail -f /var/log/auth.log | grep sftp
```

### Backup strategy:
Consider setting up automated backups of the uploads directory to another location.

## Step 8: Scaling Considerations

### For higher traffic:
- Upgrade to larger droplet (2GB RAM, 2 CPU)
- Consider load balancing with multiple droplets
- Implement CDN for file delivery

### For better performance:
- Use SSD storage
- Consider DigitalOcean Spaces (S3-compatible) for larger files
- Implement caching strategies

## Troubleshooting

### Common Issues:

1. **Connection refused**: Check firewall settings and SSH service
2. **Permission denied**: Verify directory permissions and ownership
3. **Authentication failed**: Check username/password or SSH key setup

### Useful Commands:

```bash
# Check SSH service status
systemctl status ssh

# Check SFTP user
id normaize-sftp

# Check directory permissions
ls -la /home/normaize-sftp/

# View SSH logs
tail -f /var/log/auth.log
```

## Cost Estimation

- **Basic Droplet**: $6/month (1GB RAM, 25GB SSD)
- **Backup**: $1-2/month (optional)
- **Domain**: $10-15/year (optional)

**Total**: ~$6-8/month for reliable file storage

## Alternative: DigitalOcean Spaces

For larger scale or better performance, consider DigitalOcean Spaces (S3-compatible):

1. Create a Space in DigitalOcean
2. Generate API keys
3. Use S3 storage service instead of SFTP
4. Cost: $5/month for 250GB storage + bandwidth

This setup provides a cost-effective, reliable file storage solution for Normaize using DigitalOcean infrastructure. 