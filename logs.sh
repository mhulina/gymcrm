#!/bin/bash

# Color codes
BLUE='\033[0;34m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

# Function to detect running compose file
detect_compose_file() {
    # Check for test environment
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.Database.Test"; then
        echo "docker-compose.test.yml"
        return
    fi
    
    # Check for nginx (production/dev-nginx)
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.Nginx"; then
        if [ -f "docker-compose.prod.yml" ]; then
            echo "docker-compose.prod.yml"
            return
        elif [ -f "docker-compose.dev-nginx.yml" ]; then
            echo "docker-compose.dev-nginx.yml"
            return
        fi
    fi

    # Check for dev (container names end in .Dev)
    if docker ps --format '{{.Names}}' | grep -q "\.Dev$"; then
        echo "docker-compose.dev.yml"
        return
    fi

    # Default
    echo "docker-compose.yaml"
}

# Function to detect environment name
detect_environment() {
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.Database.Test"; then
        echo "test"
        return
    fi
    
    if docker ps --format '{{.Names}}' | grep -q "GymCRM.Api"; then
        local container=$(docker ps --format '{{.Names}}' | grep "GymCRM.Api" | head -1)
        local env_var=$(docker exec "$container" printenv ASPNETCORE_ENVIRONMENT 2>/dev/null || echo "")
        if [ -n "$env_var" ]; then
            echo "$env_var" | tr '[:upper:]' '[:lower:]'
            return
        fi
    fi
    
    echo "unknown"
}

# Detect compose file and environment
COMPOSE_FILE=$(detect_compose_file)
ENV_NAME=$(detect_environment)

# Check if any GymCRM services are running
if ! docker ps --format '{{.Names}}' | grep -q "GymCRM"; then
    echo -e "${YELLOW}⚠️  No GymCRM services are currently running${NC}"
    echo ""
    echo "Start services with:"
    echo "  ./deploy.sh -e development"
    echo "  ./deploy.sh -e production"
    echo "  ./deploy.sh -e test"
    exit 0
fi

echo -e "${BLUE}📝 Viewing logs for ${GREEN}${ENV_NAME}${NC} ${BLUE}environment${NC}"
echo -e "${BLUE}Using compose file: ${CYAN}${COMPOSE_FILE}${NC}"
echo ""

# If no service specified, show all
if [ $# -eq 0 ]; then
    echo -e "${YELLOW}Following logs for all services (Ctrl+C to stop)${NC}"
    echo ""
    docker compose -f "$COMPOSE_FILE" logs -f
else
    # Show logs for specific service
    SERVICE=$1
    
    # Try to find container by service name
    CONTAINER=$(docker ps --format '{{.Names}}' | grep -i "$SERVICE" | head -1)
    
    if [ -z "$CONTAINER" ]; then
        echo -e "${YELLOW}⚠️  Service '${SERVICE}' not found${NC}"
        echo ""
        echo -e "${CYAN}Available services:${NC}"
        docker ps --format '{{.Names}}' | grep "GymCRM"
        exit 1
    fi
    
    echo -e "${YELLOW}Following logs for service: ${GREEN}${CONTAINER}${NC}"
    echo ""
    docker logs -f "$CONTAINER"
fi