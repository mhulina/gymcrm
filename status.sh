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
    elif docker ps --format '{{.Names}}' | grep -q "\.Dev$"; then
        echo "docker-compose.dev.yml"
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
    
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.Api"; then
        local container=$(docker ps --format '{{.Names}}' | grep "GymCRM.Api" | head -1)
        local env_var=$(docker exec "$container" printenv ASPNETCORE_ENVIRONMENT 2>/dev/null || echo "Development")
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
docker compose -f "$COMPOSE_FILE" ps 2>/dev/null || docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | grep -E "NAMES|GymCRM"
echo ""

# Health Checks - dynamically detect services and their ports
echo -e "${YELLOW}🏥 Health Checks:${NC}"

# API (Identity + Scheduling modules, one process)
API_CONTAINER=$(docker ps --format '{{.Names}}' | grep "GymCRM.Api" | head -1)
if [ -n "$API_CONTAINER" ]; then
    # Try to get the actual exposed port
    API_PORT=$(docker port "$API_CONTAINER" 2>/dev/null | grep "8080/tcp" | sed 's/.*://' | head -1)

    if [ -z "$API_PORT" ]; then
        # Try from environment
        API_PORT="${API_HTTP_PORT}"
    fi

    # Try from REACT_APP_IDENTITY_API_URL / REACT_APP_SCHEDULING_API_URL (both point at the same API now)
    if [ -z "$API_PORT" ] && [ -n "${REACT_APP_IDENTITY_API_URL}" ]; then
        API_PORT=$(get_port_from_url "${REACT_APP_IDENTITY_API_URL}")
    fi
    if [ -z "$API_PORT" ] && [ -n "${REACT_APP_SCHEDULING_API_URL}" ]; then
        API_PORT=$(get_port_from_url "${REACT_APP_SCHEDULING_API_URL}")
    fi

    API_HOST=$(get_host_from_url "${REACT_APP_IDENTITY_API_URL:-http://localhost}")

    echo -n "  API: "
    if [ -n "$API_PORT" ]; then
        if curl -sf "http://localhost:${API_PORT}/health" > /dev/null 2>&1; then
            echo -e "${GREEN}✅ Healthy${NC} (port ${API_PORT})"
        else
            echo -e "${RED}❌ Unhealthy${NC} (port ${API_PORT})"
        fi
    else
        echo -e "${YELLOW}⚠️  Port not detected${NC}"
    fi
fi

# Database
DB_CONTAINER=$(docker ps --format '{{.Names}}' | grep "GymCRM.Database" | head -1)
if [ -n "$DB_CONTAINER" ]; then
    DB_USER=$(get_container_env "$DB_CONTAINER" "POSTGRES_USER")
    echo -n "  PostgreSQL: "
    if docker exec "$DB_CONTAINER" pg_isready -U "${DB_USER:-postgres}" > /dev/null 2>&1; then
        DB_PORT=$(docker port "$DB_CONTAINER" 2>/dev/null | grep "5432/tcp" | sed 's/.*://' | head -1)
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

# API URLs (Identity + Scheduling modules, one process)
if [ -n "$API_CONTAINER" ]; then
    echo -e "  ${GREEN}API:${NC}"

    # External URL
    if [ -n "${REACT_APP_IDENTITY_API_URL}" ]; then
        echo -e "    External:   ${REACT_APP_IDENTITY_API_URL}"
        echo -e "    Swagger:    ${REACT_APP_IDENTITY_API_URL}/swagger"
    elif [ -n "$API_PORT" ]; then
        echo -e "    External:   http://${API_HOST:-localhost}:${API_PORT}"
        echo -e "    Swagger:    http://${API_HOST:-localhost}:${API_PORT}/swagger"
    fi

    echo -e "    Internal:   http://api:8080"
fi

# Web App
WEBAPP_CONTAINER=$(docker ps --format '{{.Names}}' | grep "GymCRMWebApp" | head -1)
if [ -n "$WEBAPP_CONTAINER" ]; then
    WEBAPP_PORT=$(docker port "$WEBAPP_CONTAINER" 2>/dev/null | grep "3000/tcp" | sed 's/.*://' | head -1)
    WEBAPP_HOST=$(get_host_from_url "${REACT_APP_API_URL:-http://localhost}")

    echo -e "  ${GREEN}Web App:${NC}"
    if [ -n "$WEBAPP_PORT" ]; then
        echo -e "    URL:        http://${WEBAPP_HOST}:${WEBAPP_PORT}"
    fi
fi

# Database
if [ -n "$DB_CONTAINER" ]; then
    DB_PORT=$(docker port "$DB_CONTAINER" 2>/dev/null | grep "5432/tcp" | sed 's/.*://' | head -1)
    DB_USER=$(get_container_env "$DB_CONTAINER" "POSTGRES_USER")
    DB_NAME=$(get_container_env "$DB_CONTAINER" "POSTGRES_DB")

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

if [ -n "$API_CONTAINER" ]; then
    ASP_ENV=$(get_container_env "$API_CONTAINER" "ASPNETCORE_ENVIRONMENT")
    MAX_ATTEMPTS=$(get_container_env "$API_CONTAINER" "Security__MaxFailedLoginAttempts")
    LOCKOUT_MIN=$(get_container_env "$API_CONTAINER" "Security__LockoutDurationMinutes")
    AUTH_RATE=$(get_container_env "$API_CONTAINER" "Security__AuthRateLimitPerMinute")

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