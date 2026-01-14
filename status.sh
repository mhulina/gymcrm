#!/bin/bash

# Color codes
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m'

# Function to detect compose file
detect_compose_file() {
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.Database.Test"; then
        echo "docker-compose.test.yml"
    elif docker ps --format '{{.Names}}' | grep -q "GymCRM.Nginx"; then
        if [ -f "docker-compose.prod.yml" ]; then
            echo "docker-compose.prod.yml"
        elif [ -f "docker-compose.dev-nginx.yml" ]; then
            echo "docker-compose.dev-nginx.yml"
        else
            echo "docker-compose.yaml"
        fi
    else
        echo "docker-compose.yaml"
    fi
}

# Function to detect environment
detect_environment() {
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.Database.Test"; then
        echo "TEST"
        return
    fi
    
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.IdentityAPI"; then
        local env_var=$(docker exec GymCRM.IdentityAPI printenv ASPNETCORE_ENVIRONMENT 2>/dev/null || echo "Development")
        echo "${env_var^^}"  # Convert to uppercase
    else
        echo "UNKNOWN"
    fi
}

# Function to get environment variable from container
get_container_env() {
    local container=$1
    local var_name=$2
    docker exec "$container" printenv "$var_name" 2>/dev/null || echo ""
}

# Function to extract host and port from URL
get_host_from_url() {
    local url=$1
    echo "$url" | sed -E 's|https?://([^:/]+).*|\1|'
}

get_port_from_url() {
    local url=$1
    local port=$(echo "$url" | sed -E 's|https?://[^:]+:([0-9]+).*|\1|')
    if [[ "$port" =~ ^[0-9]+$ ]]; then
        echo "$port"
    else
        # Default ports
        if [[ "$url" =~ ^https:// ]]; then
            echo "443"
        else
            echo "80"
        fi
    fi
}

# Check if any services are running
if ! docker ps --format '{{.Names}}' | grep -q "GymCRM"; then
    echo -e "${YELLOW}No GymCRM services are currently running${NC}"
    echo ""
    echo "Start services with:"
    echo "  ./deploy.sh -e development"
    echo "  ./deploy.sh -e production"
    echo "  ./deploy.sh -e test"
    exit 0
fi

COMPOSE_FILE=$(detect_compose_file)
ENV_NAME=$(detect_environment)

# Try to detect and load the corresponding .env file
ENV_FILE=""
case "$ENV_NAME" in
    "DEVELOPMENT")
        ENV_FILE=".env.development"
        ;;
    "PRODUCTION")
        ENV_FILE=".env.production"
        ;;
    "TEST")
        ENV_FILE=".env.test"
        ;;
esac

# Load environment variables if file exists
if [ -n "$ENV_FILE" ] && [ -f "$ENV_FILE" ]; then
    set -a
    source "$ENV_FILE"
    set +a
fi

echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║${NC}  ${GREEN}📊 GymCRM Status - ${ENV_NAME} Environment${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${CYAN}Compose File:${NC} ${COMPOSE_FILE}"
if [ -n "$ENV_FILE" ]; then
    echo -e "${CYAN}Env File:${NC}     ${ENV_FILE}"
fi
echo ""

# Container Status
echo -e "${YELLOW}🐳 Container Status:${NC}"
docker-compose -f "$COMPOSE_FILE" ps 2>/dev/null || docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | grep -E "NAMES|GymCRM"
echo ""

# Health Checks - dynamically detect services and their ports
echo -e "${YELLOW}🏥 Health Checks:${NC}"

# Identity API
if docker ps --format '{{.Names}}' | grep -q "GymCRM.IdentityAPI"; then
    # Try to get the actual exposed port
    IDENTITY_PORT=$(docker port GymCRM.IdentityAPI 2>/dev/null | grep "8080/tcp" | sed 's/.*://' | head -1)
    
    if [ -z "$IDENTITY_PORT" ]; then
        # Try from environment
        IDENTITY_PORT="${IDENTITY_API_HTTP_PORT}"
    fi
    
    # Try from REACT_APP_IDENTITY_API_URL
    if [ -z "$IDENTITY_PORT" ] && [ -n "${REACT_APP_IDENTITY_API_URL}" ]; then
        IDENTITY_PORT=$(get_port_from_url "${REACT_APP_IDENTITY_API_URL}")
    fi
    
    IDENTITY_HOST=$(get_host_from_url "${REACT_APP_IDENTITY_API_URL:-http://localhost}")
    
    echo -n "  Identity API: "
    if [ -n "$IDENTITY_PORT" ]; then
        if curl -sf "http://localhost:${IDENTITY_PORT}/health" > /dev/null 2>&1; then
            echo -e "${GREEN}✅ Healthy${NC} (port ${IDENTITY_PORT})"
        else
            echo -e "${RED}❌ Unhealthy${NC} (port ${IDENTITY_PORT})"
        fi
    else
        echo -e "${YELLOW}⚠️  Port not detected${NC}"
    fi
fi

# Scheduling API
if docker ps --format '{{.Names}}' | grep -q "GymCRM.SchedulingAPI"; then
    SCHEDULING_PORT=$(docker port GymCRM.SchedulingAPI 2>/dev/null | grep "8080/tcp" | sed 's/.*://' | head -1)
    
    if [ -z "$SCHEDULING_PORT" ]; then
        SCHEDULING_PORT="${SCHEDULING_API_HTTP_PORT}"
    fi
    
    if [ -z "$SCHEDULING_PORT" ] && [ -n "${REACT_APP_SCHEDULING_API_URL}" ]; then
        SCHEDULING_PORT=$(get_port_from_url "${REACT_APP_SCHEDULING_API_URL}")
    fi
    
    echo -n "  Scheduling API: "
    if [ -n "$SCHEDULING_PORT" ]; then
        if curl -sf "http://localhost:${SCHEDULING_PORT}/health" > /dev/null 2>&1; then
            echo -e "${GREEN}✅ Healthy${NC} (port ${SCHEDULING_PORT})"
        else
            echo -e "${RED}❌ Unhealthy${NC} (port ${SCHEDULING_PORT})"
        fi
    else
        echo -e "${YELLOW}⚠️  Port not detected${NC}"
    fi
fi

# Database
if docker ps --format '{{.Names}}' | grep -q "GymCRM.Database"; then
    DB_USER=$(get_container_env "GymCRM.Database" "POSTGRES_USER")
    echo -n "  PostgreSQL: "
    if docker exec GymCRM.Database pg_isready -U "${DB_USER:-postgres}" > /dev/null 2>&1; then
        DB_PORT=$(docker port GymCRM.Database 2>/dev/null | grep "5432/tcp" | sed 's/.*://' | head -1)
        echo -e "${GREEN}✅ Ready${NC} (port ${DB_PORT:-5432})"
    else
        echo -e "${RED}❌ Not Ready${NC}"
    fi
fi

# Nginx
if docker ps --format '{{.Names}}' | grep -q "Nginx"; then
    echo -n "  Nginx Proxy: "
    if docker ps --filter "name=Nginx" --filter "status=running" | grep -q "Nginx"; then
        echo -e "${GREEN}✅ Running${NC}"
    else
        echo -e "${RED}❌ Not Running${NC}"
    fi
fi

echo ""

# Service URLs - Auto-detected
echo -e "${YELLOW}🌐 Service URLs:${NC}"

# Identity API URLs
if docker ps --format '{{.Names}}' | grep -q "GymCRM.IdentityAPI"; then
    echo -e "  ${GREEN}Identity API:${NC}"
    
    # External URL
    if [ -n "${REACT_APP_IDENTITY_API_URL}" ]; then
        echo -e "    External:   ${REACT_APP_IDENTITY_API_URL}"
        echo -e "    Swagger:    ${REACT_APP_IDENTITY_API_URL}/swagger"
    elif [ -n "$IDENTITY_PORT" ]; then
        echo -e "    External:   http://${IDENTITY_HOST:-localhost}:${IDENTITY_PORT}"
        echo -e "    Swagger:    http://${IDENTITY_HOST:-localhost}:${IDENTITY_PORT}/swagger"
    fi
    
    # Internal URL
    IDENTITY_INTERNAL=$(get_container_env "GymCRM.SchedulingAPI" "IdentityApiUrl")
    if [ -n "$IDENTITY_INTERNAL" ]; then
        echo -e "    Internal:   ${IDENTITY_INTERNAL}"
    else
        echo -e "    Internal:   http://identityapi:8080"
    fi
fi

# Scheduling API URLs
if docker ps --format '{{.Names}}' | grep -q "GymCRM.SchedulingAPI"; then
    echo -e "  ${GREEN}Scheduling API:${NC}"
    
    if [ -n "${REACT_APP_SCHEDULING_API_URL}" ]; then
        echo -e "    External:   ${REACT_APP_SCHEDULING_API_URL}"
        echo -e "    Swagger:    ${REACT_APP_SCHEDULING_API_URL}/swagger"
    elif [ -n "$SCHEDULING_PORT" ]; then
        SCHEDULING_HOST=$(get_host_from_url "${REACT_APP_SCHEDULING_API_URL:-http://localhost}")
        echo -e "    External:   http://${SCHEDULING_HOST}:${SCHEDULING_PORT}"
        echo -e "    Swagger:    http://${SCHEDULING_HOST}:${SCHEDULING_PORT}/swagger"
    fi
    
    echo -e "    Internal:   http://schedulingapi:8080"
fi

# Web App
if docker ps --format '{{.Names}}' | grep -q "GymCRMWebApp"; then
    WEBAPP_PORT=$(docker port GymCRMWebApp 2>/dev/null | grep "3000/tcp" | sed 's/.*://' | head -1)
    WEBAPP_HOST=$(get_host_from_url "${REACT_APP_API_URL:-http://localhost}")
    
    echo -e "  ${GREEN}Web App:${NC}"
    if [ -n "$WEBAPP_PORT" ]; then
        echo -e "    URL:        http://${WEBAPP_HOST}:${WEBAPP_PORT}"
    fi
fi

# Database
if docker ps --format '{{.Names}}' | grep -q "GymCRM.Database"; then
    DB_PORT=$(docker port GymCRM.Database 2>/dev/null | grep "5432/tcp" | sed 's/.*://' | head -1)
    DB_USER=$(get_container_env "GymCRM.Database" "POSTGRES_USER")
    DB_NAME=$(get_container_env "GymCRM.Database" "POSTGRES_DB")
    
    echo -e "  ${GREEN}Database:${NC}"
    if [ -n "$DB_PORT" ]; then
        echo -e "    External:   localhost:${DB_PORT}"
    fi
    echo -e "    Internal:   postgres:5432"
    [ -n "$DB_USER" ] && echo -e "    User:       ${DB_USER}"
    [ -n "$DB_NAME" ] && echo -e "    Database:   ${DB_NAME}"
fi

echo ""

# Configuration - from running containers
echo -e "${YELLOW}⚙️  Configuration:${NC}"

if docker ps --format '{{.Names}}' | grep -q "GymCRM.IdentityAPI"; then
    ASP_ENV=$(get_container_env "GymCRM.IdentityAPI" "ASPNETCORE_ENVIRONMENT")
    MAX_ATTEMPTS=$(get_container_env "GymCRM.IdentityAPI" "Security__MaxFailedLoginAttempts")
    LOCKOUT_MIN=$(get_container_env "GymCRM.IdentityAPI" "Security__LockoutDurationMinutes")
    AUTH_RATE=$(get_container_env "GymCRM.IdentityAPI" "Security__AuthRateLimitPerMinute")
    
    [ -n "$ASP_ENV" ] && echo -e "  Environment:              ${ASP_ENV}"
    [ -n "$MAX_ATTEMPTS" ] && echo -e "  Max Failed Attempts:      ${MAX_ATTEMPTS}"
    [ -n "$LOCKOUT_MIN" ] && echo -e "  Lockout Duration:         ${LOCKOUT_MIN} minutes"
    [ -n "$AUTH_RATE" ] && echo -e "  Auth Rate Limit:          ${AUTH_RATE} req/min"
fi

echo ""

# Resource Usage
echo -e "${YELLOW}💾 Resource Usage:${NC}"
RUNNING_CONTAINERS=$(docker ps --filter "name=GymCRM" --format "{{.ID}}" 2>/dev/null)
if [ -n "$RUNNING_CONTAINERS" ]; then
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}" $RUNNING_CONTAINERS 2>/dev/null
else
    echo "  No resource data available"
fi