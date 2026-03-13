# Manually setup UI on existing instance

$ErrorActionPreference = "Continue"
$env:PYTHONWARNINGS = "ignore:Unverified HTTPS request"

$INSTANCE_ID = "i-0a01b722787ee7938"
$BUCKET = "contoso-deployment-981461568039"

Write-Host "Setting up UI on instance..." -ForegroundColor Yellow

# Create a script file to upload
$setupScript = @'
#!/bin/bash
set -e

echo "Installing nginx..."
dnf install -y nginx

echo "Downloading React UI..."
cd /var/www/html
rm -rf *
aws s3 cp s3://contoso-deployment-981461568039/react-ui.zip . --no-verify-ssl
unzip -o react-ui.zip
rm react-ui.zip

echo "Configuring nginx..."
cat > /etc/nginx/conf.d/contoso.conf <<'EOF'
server {
    listen 80;
    server_name _;
    root /var/www/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://localhost:5000/api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF

echo "Starting nginx..."
systemctl enable nginx
systemctl restart nginx

echo "Done!"
ls -la /var/www/html/
systemctl status nginx --no-pager
'@

# Save script locally
$setupScript | Out-File -FilePath setup-ui.sh -Encoding ASCII

# Upload script to S3
Write-Host "Uploading setup script..." -ForegroundColor Yellow
aws s3 cp setup-ui.sh s3://$BUCKET/setup-ui.sh --no-verify-ssl 2>$null

# Run script on instance
Write-Host "Running setup on instance..." -ForegroundColor Yellow
$COMMAND_ID = (aws ssm send-command `
    --instance-ids $INSTANCE_ID `
    --document-name "AWS-RunShellScript" `
    --parameters 'commands=["aws s3 cp s3://'$BUCKET'/setup-ui.sh /tmp/ --no-verify-ssl","chmod +x /tmp/setup-ui.sh","bash /tmp/setup-ui.sh"]' `
    --output text `
    --query "Command.CommandId" `
    --no-verify-ssl 2>$null | Where-Object { $_ -and $_ -notmatch "InsecureRequestWarning" })

Write-Host "Command ID: $COMMAND_ID" -ForegroundColor Cyan
Write-Host "Waiting 15 seconds..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host ""
Write-Host "Output:" -ForegroundColor Cyan
aws ssm get-command-invocation --command-id $COMMAND_ID --instance-id $INSTANCE_ID --query "StandardOutputContent" --output text --no-verify-ssl 2>$null

Remove-Item setup-ui.sh

Write-Host ""
Write-Host "Try http://50.19.77.253 now!" -ForegroundColor Green
