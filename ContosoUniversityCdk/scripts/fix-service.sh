#!/bin/bash
# Get database credentials
DB_SECRET=$(aws secretsmanager get-secret-value --secret-id arn:aws:secretsmanager:us-east-1:981461568039:secret:DatabaseDbSecret1098DC6E-jOpdtasXdg5U-6JFMTH --region us-east-1 --query SecretString --output text --no-verify-ssl 2>/dev/null)
DB_USERNAME=$(echo "$DB_SECRET" | jq -r .username)
DB_PASSWORD=$(echo "$DB_SECRET" | jq -r .password)
DB_HOST="contosouniversitystack-databaseauroracluster35ae33-zgboy05utsxy.cluster-cglw06ya4k77.us-east-1.rds.amazonaws.com"

# Create systemd service
cat > /etc/systemd/system/contoso-api.service <<EOF
[Unit]
Description=Contoso University API
After=network.target

[Service]
Type=simple
WorkingDirectory=/opt/contoso-api
ExecStart=/usr/bin/dotnet /opt/contoso-api/ContosoUniversity.dll
StandardOutput=journal
StandardError=journal
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=contoso-api
User=root
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:80
Environment=DB_HOST=$DB_HOST
Environment=DB_NAME=contoso
Environment=DB_USERNAME=$DB_USERNAME
Environment=DB_PASSWORD=$DB_PASSWORD
Environment=AllowedHosts=*
Environment=NotificationAPI__BaseUrl=http://contoso-alb-2061956418.us-east-1.elb.amazonaws.com

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable contoso-api
systemctl start contoso-api
sleep 3
systemctl status contoso-api --no-pager
