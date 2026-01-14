#!/bin/bash

set -e

# Color codes
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${BLUE}🔧 Setting up Nginx for GymCRM${NC}"
echo ""

# Check if we're in the right directory
if [ ! -f "docker-compose.yaml" ]; then
    echo -e "${YELLOW}⚠️  Warning: docker-compose.yaml not found${NC}"
    echo "Please run this script from your project root directory"
    exit 1
fi

# Create nginx directory structure
echo -e "${YELLOW}📁 Creating nginx directory structure...${NC}"
mkdir -p nginx/logs

# Create nginx.conf
echo -e "${YELLOW}📝 Creating nginx.conf...${NC}"
cat > nginx/nginx.conf << 'EOF'
events {
    worker_connections 1024;
}

http {
    # MIME types
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    # Logging
    access_log /var/log/nginx/access.log;
    error_log /var/log/nginx/error.log warn;

    # Performance
    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    types_hash_max_size 2048;

    # Gzip
    gzip on;
    gzip_vary on;
    gzip_min_length 1000;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;

    # Upstream servers (internal Docker network)
    upstream identityapi {
        server identityapi:8080;
    }

    upstream schedulingapi {
        server schedulingapi:8080;
    }

    upstream webapp {
        server webapp:3000;
    }

    # Health check endpoint
    server {
        listen 80;
        server_name _;
        
        location /health {
            access_log off;
            return 200 "healthy\n";
            add_header Content-Type text/plain;
        }
    }

    # Identity API - identity.gymcrm.local
    server {
        listen 80;
        server_name identity.gymcrm.local;

        location / {
            # Handle preflight OPTIONS requests
            if ($request_method = 'OPTIONS') {
                add_header 'Access-Control-Allow-Origin' 'http://gymcrm.local' always;
                add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, PATCH, OPTIONS' always;
                add_header 'Access-Control-Allow-Headers' 'Content-Type, Authorization, X-Requested-With' always;
                add_header 'Access-Control-Allow-Credentials' 'true' always;
                add_header 'Access-Control-Max-Age' 1728000;
                add_header 'Content-Type' 'text/plain; charset=utf-8';
                add_header 'Content-Length' 0;
                return 204;
            }

            # CORS headers for actual requests
            add_header 'Access-Control-Allow-Origin' 'http://gymcrm.local' always;
            add_header 'Access-Control-Allow-Credentials' 'true' always;
            add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, PATCH, OPTIONS' always;
            add_header 'Access-Control-Allow-Headers' 'Content-Type, Authorization, X-Requested-With' always;

            proxy_pass http://identityapi;
            proxy_http_version 1.1;
            
            # Headers
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_set_header X-Forwarded-Host $host;
            
            # Timeouts
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
            
            # Buffer settings
            proxy_buffering off;
            proxy_request_buffering off;
        }
    }

    # Scheduling API - scheduling.gymcrm.local
    server {
        listen 80;
        server_name scheduling.gymcrm.local;

        location / {
            # Handle preflight OPTIONS requests
            if ($request_method = 'OPTIONS') {
                add_header 'Access-Control-Allow-Origin' 'http://gymcrm.local' always;
                add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, PATCH, OPTIONS' always;
                add_header 'Access-Control-Allow-Headers' 'Content-Type, Authorization, X-Requested-With' always;
                add_header 'Access-Control-Allow-Credentials' 'true' always;
                add_header 'Access-Control-Max-Age' 1728000;
                add_header 'Content-Type' 'text/plain; charset=utf-8';
                add_header 'Content-Length' 0;
                return 204;
            }

            # CORS headers for actual requests
            add_header 'Access-Control-Allow-Origin' 'http://gymcrm.local' always;
            add_header 'Access-Control-Allow-Credentials' 'true' always;
            add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, PATCH, OPTIONS' always;
            add_header 'Access-Control-Allow-Headers' 'Content-Type, Authorization, X-Requested-With' always;

            proxy_pass http://schedulingapi;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_set_header X-Forwarded-Host $host;
            
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
            
            proxy_buffering off;
            proxy_request_buffering off;
        }
    }

    # Web Application - gymcrm.local
    server {
        listen 80;
        server_name gymcrm.local www.gymcrm.local;

        location / {
            proxy_pass http://webapp;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_set_header X-Forwarded-Host $host;
            
            # WebSocket support for React Hot Reload
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
            
            proxy_buffering off;
        }
    }
}
EOF

# Create .gitkeep for logs directory
touch nginx/logs/.gitkeep

# Create .gitignore entry for logs
if [ -f ".gitignore" ]; then
    if ! grep -q "nginx/logs/\*.log" .gitignore; then
        echo "" >> .gitignore
        echo "# Nginx logs" >> .gitignore
        echo "nginx/logs/*.log" >> .gitignore
        echo "!nginx/logs/.gitkeep" >> .gitignore
        echo -e "${GREEN}✅ Added nginx logs to .gitignore${NC}"
    fi
fi

echo ""
echo -e "${GREEN}✅ Nginx setup complete!${NC}"
echo ""
echo -e "${BLUE}Directory structure created:${NC}"
echo "  nginx/"
echo "  ├── nginx.conf"
echo "  └── logs/"
echo "      └── .gitkeep"
echo ""
echo -e "${YELLOW}📋 Next steps:${NC}"
echo ""
echo "1. Add these lines to your hosts file:"
echo "   ${BLUE}(Linux/Mac: /etc/hosts | Windows: C:\\Windows\\System32\\drivers\\etc\\hosts)${NC}"
echo ""
echo "   127.0.0.1    gymcrm.local"
echo "   127.0.0.1    www.gymcrm.local"
echo "   127.0.0.1    identity.gymcrm.local"
echo "   127.0.0.1    scheduling.gymcrm.local"
echo ""
echo "2. Deploy with nginx:"
echo "   ${GREEN}./deploy.sh -e development -f docker-compose.dev-nginx.yml${NC}"
echo ""
echo "3. Access your services:"
echo "   Web App:         ${GREEN}http://gymcrm.local${NC}"
echo "   Identity API:    ${GREEN}http://identity.gymcrm.local/swagger${NC}"
echo "   Scheduling API:  ${GREEN}http://scheduling.gymcrm.local/swagger${NC}"
echo ""