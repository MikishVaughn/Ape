# Ape Framework - Email Configuration Guide

## Introduction

The Ape Framework uses a dual-provider email system for reliability. Azure Communication Services serves as the primary provider, with SMTP as an automatic fallback. This guide covers configuring both providers.

---

## Email System Overview

### Dual-Provider Architecture

```
┌─────────────────┐
│  Send Email     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐     Success    ┌──────────────┐
│  Azure Email    │───────────────►│  Email Sent  │
└────────┬────────┘                └──────────────┘
         │ Failure
         ▼
┌─────────────────┐     Success    ┌──────────────┐
│  SMTP Fallback  │───────────────►│  Email Sent  │
└────────┬────────┘                └──────────────┘
         │ Failure
         ▼
┌─────────────────┐
│  Logged Error   │
└─────────────────┘
```

### Automatic Failover

- If Azure fails 3 consecutive times, it's skipped for 2 minutes
- SMTP becomes the primary during this cooldown period
- All attempts are logged with provider used and any error messages

---

## Azure Communication Services Setup

### Step 1: Create Azure Resources

1. Log in to [Azure Portal](https://portal.azure.com)
2. Create a new **Communication Services** resource:
   - Click "Create a resource"
   - Search for "Communication Services"
   - Click "Create"
   - Fill in:
     - Subscription: Your subscription
     - Resource group: Create new or select existing
     - Resource name: e.g., "ape-email-service"
     - Data location: Select your region
   - Click "Review + create" → "Create"

### Step 2: Set Up Email Domain

1. In your Communication Services resource, go to **Email** → **Domains**
2. Click **Add domain**
3. Choose one of:
   - **Azure managed domain:** Quick setup, uses `@azurecomm.net` address
   - **Custom domain:** Use your own domain (requires DNS configuration)

**For Azure Managed Domain:**
1. Select "Azure managed domain"
2. Click "Add"
3. Note the sender email address provided (e.g., `DoNotReply@xxxxxxxx.azurecomm.net`)

**For Custom Domain:**
1. Select "Custom domain"
2. Enter your domain name
3. Add the required DNS records (SPF, DKIM, DMARC)
4. Verify the domain
5. Add sender addresses (e.g., `noreply@yourdomain.com`)

### Step 3: Get Connection String

1. In your Communication Services resource
2. Go to **Keys** (under Settings)
3. Copy the **Connection string** (either primary or secondary)

### Step 4: Configure in Ape Framework

1. Log in as Admin
2. Navigate to **System Credentials**
3. Add these credentials:

| Key | Category | Value |
|-----|----------|-------|
| `AZURE_COMMUNICATION_CONNECTION_STRING` | Email | Your connection string |
| `AZURE_EMAIL_FROM` | Email | Your verified sender address |

---

## SMTP Configuration

SMTP serves as the fallback (or can be used as the primary if Azure is not configured).

### Common SMTP Providers

#### Gmail
```
Server: smtp.gmail.com
Port: 587
SSL: true
Username: your-email@gmail.com
Password: App Password (not your regular password)
```

**Important:** Gmail requires an "App Password" for SMTP:
1. Enable 2-factor authentication on your Google account
2. Go to Google Account → Security → App passwords
3. Generate a new app password for "Mail"
4. Use this password (not your regular Gmail password)

#### Microsoft 365 / Outlook
```
Server: smtp.office365.com
Port: 587
SSL: true
Username: your-email@yourdomain.com
Password: Your password or app password
```

#### Amazon SES
```
Server: email-smtp.us-east-1.amazonaws.com (region-specific)
Port: 587
SSL: true
Username: SMTP username from SES console
Password: SMTP password from SES console
```

#### SendGrid
```
Server: smtp.sendgrid.net
Port: 587
SSL: true
Username: apikey
Password: Your SendGrid API key
```

### Configure in Ape Framework

1. Log in as Admin
2. Navigate to **System Credentials**
3. Add these credentials:

| Key | Category | Value |
|-----|----------|-------|
| `SMTP_SERVER` | Email | SMTP server address |
| `SMTP_PORT` | Email | Port number (usually 587) |
| `SMTP_USERNAME` | Email | Your username/email |
| `SMTP_PASSWORD` | Email | Your password/app password |
| `SMTP_SSL` | Email | true |

---

## Site Name Configuration

Set your site name for email templates:

| Key | Category | Value |
|-----|----------|-------|
| `SITE_NAME` | Site | Your Site Name |

This appears in email subjects and templates (e.g., "Welcome to Your Site Name").

---

## Testing Your Configuration

### Using the Email Test Page

1. Navigate to **Admin** → **Email Test**
2. Enter a recipient email address

**Test Azure:**
- Click "Test Azure Email"
- Check for success message
- Verify email received

**Test SMTP:**
- Click "Test SMTP Email"
- Check for success message
- Verify email received

### Checking Email Logs

1. Navigate to **Admin** → **Email Logs**
2. Find your test emails
3. Verify:
   - Status shows "Success"
   - Correct provider was used
   - No error messages

---

## Email Types in the System

The framework sends these types of emails:

| Type | Purpose | Trigger |
|------|---------|---------|
| Confirmation | Email verification | User registration |
| Welcome | Welcome message | After email confirmed |
| PasswordReset | Reset password link | Forgot password request |
| TwoFactorCode | 2FA verification | 2FA login |
| ContactForm | Contact submissions | Public contact form |
| AdminNotification | System alerts | Various admin events |

---

## Contact Form Email Recipients

Configure who receives contact form submissions:

1. Navigate to **Admin** → **Contact Form Settings**
2. Enter email addresses separated by commas:
   ```
   admin@example.com, support@example.com, sales@example.com
   ```
3. Click **Save**

If no recipients are configured, emails go to the `SMTP_USERNAME` address.

---

## Troubleshooting

### Azure Email Issues

**"Connection string is invalid"**
- Verify the connection string is complete
- Check for extra spaces or line breaks
- Ensure you copied the full string

**"Sender not authorized"**
- Verify the sender email is verified in Azure
- For custom domains, ensure DNS records are correct
- Check the domain status shows "Verified"

**"Rate limit exceeded"**
- Azure has sending limits based on your plan
- Implement email queuing for high volume
- Consider upgrading your Azure plan

### SMTP Issues

**"Authentication failed"**
- Verify username and password
- For Gmail, ensure you're using an App Password
- Check if your email provider requires app-specific passwords

**"Connection refused"**
- Verify server address and port
- Check firewall allows outbound on port 587/465
- Try port 465 with SSL if 587 fails

**"Certificate error"**
- Ensure `SMTP_SSL` is set correctly
- Some servers require different SSL settings
- Try setting `SMTP_SSL` to false for testing

### General Issues

**Emails going to spam**
- Set up SPF, DKIM, and DMARC for your domain
- Use a verified sender address
- Avoid spam trigger words in content

**No emails at all**
1. Check Email Logs for errors
2. Test both Azure and SMTP individually
3. Verify credentials are entered correctly
4. Check firewall/network settings

---

## Best Practices

### Reliability

1. **Configure both providers** - Ensures emails send even if one fails
2. **Monitor email logs** - Regularly check for failures
3. **Test after changes** - Always test after updating credentials

### Deliverability

1. **Use verified domains** - Configure SPF, DKIM, DMARC
2. **Consistent sender** - Use the same "from" address
3. **Monitor bounces** - Track failed deliveries

### Security

1. **Use app passwords** - Don't use main account passwords
2. **Rotate credentials** - Update passwords periodically
3. **Limit access** - Only admins should access email settings

---

## Email Template Customization

Email templates are defined in the `EnhancedEmailService`. To customize:

1. Locate `Services/EnhancedEmailService.cs`
2. Find the relevant method (e.g., `SendEmailAsync`)
3. Modify the HTML template as needed
4. Rebuild and deploy

Common customizations:
- Company logo
- Color scheme
- Footer information
- Legal disclaimers

---

**Version:** 1.0.0
**Framework:** Ape Framework
**Site:** https://Illustrate.net
