#!/bin/bash

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m'

# Default values
ENVIRONMENT="development"
COMPOSE_FILE="docker-compose.yaml"
ENV_FILE=""
DETACHED="-d"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -f|--file)
            COMPOSE_FILE="$2"
            shift 2
            ;;
        --env-file)
            ENV_FILE="$2"
            shift 2
            ;;
        --no-detach)
            DETACHED=""
            shift
            ;;
        -h|--help)
            echo "Usage: ./deploy.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -e, --environment ENV    Environment name (default: development)"
            echo "  -f, --file FILE          Docker compose file (auto-detected if not specified)"
            echo "  --env-file FILE          Environment file (auto-detected if not specified)"
            echo "  --no-detach              Run in foreground (don't detach)"
            echo "  -h, --help               Show this help message"
            echo ""
            echo "Examples:"
            echo "  ./deploy.sh                              # Auto-detect development"
            echo "  ./deploy.sh -e production                # Deploy production"
            echo "  ./deploy.sh -e test                      # Deploy test"
            echo "  ./deploy.sh -f docker-compose.prod.yml --env-file .env.production"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use -h or --help for usage information"
            exit 1
            ;;
    esac
done

# Auto-detect environment file if not specified
if [ -z "$ENV_FILE" ]; then
    ENV_FILE=".env.${ENVIRONMENT}"
fi

# Auto-detect compose file for test environment
if [ "$ENVIRONMENT" = "test" ] && [ "$COMPOSE_FILE" = "docker-compose.yaml" ]; then
    if [ -f "docker-compose.test.yml" ]; then
        COMPOSE_FILE="docker-compose.test.yml"
    fi
fi

# Auto-detect compose file for production with nginx
if [ "$ENVIRONMENT" = "production" ] && [ -f "docker-compose.prod.yml" ]; then
    COMPOSE_FILE="docker-compose.prod.yml"
fi

# Check if environment file exists
if [ ! -f "$ENV_FILE" ]; then
    echo -e "${RED}❌ Error: Environment file '$ENV_FILE' not found${NC}"
    echo -e "${YELLOW}💡 Available environment files:${NC}"
    find . -maxdepth 1 -name ".env.*" -type f | sed 's|./||' | sort
    exit 1
fi

# Check if compose file exists
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}❌ Error: Docker compose file '$COMPOSE_FILE' not found${NC}"
    echo -e "${YELLOW}💡 Available compose files:${NC}"
    find . -maxdepth 1 -name "docker-compose*.yml" -o -name "docker-compose*.yaml" | sed 's|./||' | sort
    exit 1
fi

# Load environment variables from file
set -a
source "$ENV_FILE"
set +a

# Function to get actual deployed hostnames from running containers
get_deployed_hostname() {
    local service=$1
    local container_name=$2
    local port=$3
    
    # Try to get hostname from running container environment
    if docker ps --format '{{.Names}}' | grep -q "$container_name"; then
        local hostname=$(docker exec "$container_name" printenv 2>/dev/null | grep -E "HOSTNAME|HOST" | head -1 | cut -d'=' -f2 || echo "")
        if [ -n "$hostname" ]; then
            echo "$hostname"
            return
        fi
    fi
    
    # Fall back to environment variables
    case $service in
        identityapi)
            echo "${IDENTITY_API_EXTERNAL_HOST:-localhost}"
            ;;
        schedulingapi)
            echo "${SCHEDULING_API_EXTERNAL_HOST:-localhost}"
            ;;
        webapp)
            echo "${WEBAPP_EXTERNAL_HOST:-localhost}"
            ;;
        postgres)
            echo "${DB_EXTERNAL_HOST:-localhost}"
            ;;
        *)
            echo "localhost"
            ;;
    esac
}

# Function to check if using nginx
is_using_nginx() {
    grep -q "nginx:" "$COMPOSE_FILE" 2>/dev/null && return 0 || return 1
}

# Display deployment header
echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║${NC}  ${GREEN}🚀 GymCRM Deployment${NC}                                        ${BLUE}║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${YELLOW}📋 Configuration:${NC}"
echo -e "   Environment:     ${GREEN}${ENVIRONMENT}${NC}"
echo -e "   Compose File:    ${COMPOSE_FILE}"
echo -e "   Env File:        ${ENV_FILE}"
echo -e "   ASP.NET ENV:     ${ASPNETCORE_ENVIRONMENT:-Production}"
if is_using_nginx; then
    echo -e "   Proxy:           ${CYAN}Nginx Reverse Proxy${NC}"
else
    echo -e "   Proxy:           ${CYAN}Direct Port Mapping${NC}"
fi
echo ""

# Display security settings if they exist
if [ -n "${Security__MaxFailedLoginAttempts}" ]; then
    echo -e "${YELLOW}🔐 Security Settings:${NC}"
    echo -e "   Max Failed Attempts:       ${Security__MaxFailedLoginAttempts}"
    echo -e "   Lockout Duration:          ${Security__LockoutDurationMinutes} minutes"
    echo -e "   Auth Rate Limit:           ${Security__AuthRateLimitPerMinute} req/min"
    echo -e "   Register Rate Limit:       ${Security__RegisterRateLimitPerMinute} req/min"
    echo ""
fi

# Display database configuration
echo -e "${YELLOW}🗄️  Database:${NC}"
echo -e "   External:        ${DB_EXTERNAL_HOST:-localhost}:${POSTGRES_PORT:-5432}"
echo -e "   Internal:        ${DB_INTERNAL_HOST:-postgres}:5432"
echo -e "   User:            ${POSTGRES_USER:-postgres}"
echo -e "   Database:        ${POSTGRES_DB:-gymcrm}"
if [ -n "${IDENTITY_DB_NAME}" ]; then
    echo -e "   Identity DB:     ${IDENTITY_DB_NAME}"
fi
if [ -n "${SCHEDULING_DB_NAME}" ]; then
    echo -e "   Scheduling DB:   ${SCHEDULING_DB_NAME}"
fi
echo ""

# Stop existing containers
echo -e "${YELLOW}🛑 Stopping existing containers...${NC}"
docker-compose -f "$COMPOSE_FILE" down -v 2>/dev/null || true
echo ""

# Build and start services
echo -e "${YELLOW}🔨 Building and starting services...${NC}"
docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up --build $DETACHED

# If running detached, show service info
if [ -n "$DETACHED" ]; then
    echo ""
    echo -e "${GREEN}✅ Deployment complete!${NC}"
    echo ""
    
    # Wait for services to start
    sleep 3
    
    # Display services
    echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║${NC}  ${GREEN}📊 Running Services${NC}                                        ${BLUE}║${NC}"
    echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
    echo ""
    
    # Get actual deployed configuration
    IDENTITY_HOST=$(get_deployed_hostname "identityapi" "GymCRM.IdentityAPI" "${IDENTITY_API_HTTP_PORT}")
    SCHEDULING_HOST=$(get_deployed_hostname "schedulingapi" "GymCRM.SchedulingAPI" "${SCHEDULING_API_HTTP_PORT}")
    WEBAPP_HOST=$(get_deployed_hostname "webapp" "GymCRMWebApp" "${WEBAPP_PORT}")
    DB_HOST=$(get_deployed_hostname "postgres" "GymCRM.Database" "${POSTGRES_PORT}")
    
    # Identity API
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.IdentityAPI"; then
        echo -e "${GREEN}  🔐 Identity API${NC}"
        echo -e "     ${CYAN}External:${NC}"
        if [ -n "${IDENTITY_API_HTTP_PORT}" ]; then
            echo -e "       HTTP:     http://${IDENTITY_HOST}:${IDENTITY_API_HTTP_PORT}"
        fi
        if [ -n "${IDENTITY_API_HTTPS_PORT}" ]; then
            echo -e "       HTTPS:    https://${IDENTITY_HOST}:${IDENTITY_API_HTTPS_PORT}"
        fi
        if [ -n "${IDENTITY_API_HTTP_PORT}" ]; then
            echo -e "       Swagger:  http://${IDENTITY_HOST}:${IDENTITY_API_HTTP_PORT}/swagger"
        fi
        echo -e "     ${CYAN}Internal:${NC}"
        echo -e "       HTTP:     http://${IDENTITY_API_INTERNAL_HOST:-identityapi}:8080"
        echo ""
    fi
    
    # Scheduling API
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.SchedulingAPI"; then
        echo -e "${GREEN}  📅 Scheduling API${NC}"
        echo -e "     ${CYAN}External:${NC}"
        if [ -n "${SCHEDULING_API_HTTP_PORT}" ]; then
            echo -e "       HTTP:     http://${SCHEDULING_HOST}:${SCHEDULING_API_HTTP_PORT}"
        fi
        if [ -n "${SCHEDULING_API_HTTPS_PORT}" ]; then
            echo -e "       HTTPS:    https://${SCHEDULING_HOST}:${SCHEDULING_API_HTTPS_PORT}"
        fi
        if [ -n "${SCHEDULING_API_HTTP_PORT}" ]; then
            echo -e "       Swagger:  http://${SCHEDULING_HOST}:${SCHEDULING_API_HTTP_PORT}/swagger"
        fi
        echo -e "     ${CYAN}Internal:${NC}"
        echo -e "       HTTP:     http://${SCHEDULING_API_INTERNAL_HOST:-schedulingapi}:8080"
        echo ""
    fi
    
    # Web App
    if docker ps --format '{{.Names}}' | grep -q "GymCRMWebApp"; then
        echo -e "${GREEN}  🌐 Web Application${NC}"
        echo -e "     ${CYAN}External:${NC}"
        if [ -n "${WEBAPP_PORT}" ]; then
            echo -e "       URL:      http://${WEBAPP_HOST}:${WEBAPP_PORT}"
        fi
        if [ -n "${REACT_APP_IDENTITY_API_URL}" ] || [ -n "${REACT_APP_SCHEDULING_API_URL}" ]; then
            echo -e "     ${CYAN}API Endpoints (from browser):${NC}"
            [ -n "${REACT_APP_IDENTITY_API_URL}" ] && echo -e "       Identity:   ${REACT_APP_IDENTITY_API_URL}"
            [ -n "${REACT_APP_SCHEDULING_API_URL}" ] && echo -e "       Scheduling: ${REACT_APP_SCHEDULING_API_URL}"
        fi
        echo ""
    fi
    
    # Nginx (if present)
    if docker ps --format '{{.Names}}' | grep -q "Nginx"; then
        echo -e "${GREEN}  🔀 Nginx Reverse Proxy${NC}"
        echo -e "     ${CYAN}Status:${NC}       Running"
        echo -e "     ${CYAN}HTTP Port:${NC}   80"
        echo -e "     ${CYAN}HTTPS Port:${NC}  443"
        echo ""
    fi
    
    # Database
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.Database"; then
        echo -e "${GREEN}  🗄️  PostgreSQL Database${NC}"
        echo -e "     ${CYAN}External:${NC}"
        if [ -n "${POSTGRES_PORT}" ]; then
            echo -e "       Host:     ${DB_HOST}:${POSTGRES_PORT}"
        fi
        [ -n "${POSTGRES_USER}" ] && echo -e "       User:     ${POSTGRES_USER}"
        [ -n "${POSTGRES_DB}" ] && echo -e "       DB:       ${POSTGRES_DB}"
        echo -e "     ${CYAN}Internal:${NC}"
        echo -e "       Host:     ${DB_INTERNAL_HOST:-postgres}:5432"
        echo ""
    fi
    
    echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║${NC}  ${GREEN}📝 Useful Commands${NC}                                        ${BLUE}║${NC}"
    echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
    echo ""
    echo -e "  ${YELLOW}View logs:${NC}         ./logs.sh"
    echo -e "  ${YELLOW}Stop services:${NC}     ./stop.sh"
    echo -e "  ${YELLOW}Service status:${NC}    ./status.sh"
    
    # Dynamic health check command
    if [ -n "${IDENTITY_API_HTTP_PORT}" ]; then
        echo -e "  ${YELLOW}Health check:${NC}      curl http://${IDENTITY_HOST}:${IDENTITY_API_HTTP_PORT}/health"
    fi
    echo ""
else
    echo ""
    echo -e "${GREEN}✅ Services starting in foreground mode...${NC}"
    echo -e "${YELLOW}Press Ctrl+C to stop${NC}"
fi