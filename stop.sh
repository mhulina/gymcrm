#!/bin/bash

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

REMOVE_VOLUMES=false
COMPOSE_FILE=""

# Function to detect running compose file
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

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -v|--volumes)
            REMOVE_VOLUMES=true
            shift
            ;;
        -f|--file)
            COMPOSE_FILE="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: ./stop.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -v, --volumes     Remove volumes (deletes all data)"
            echo "  -f, --file FILE   Docker compose file (auto-detected if not specified)"
            echo "  -h, --help        Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Auto-detect compose file if not specified
if [ -z "$COMPOSE_FILE" ]; then
    COMPOSE_FILE=$(detect_compose_file)
fi

# Check if services are running
if ! docker ps --format '{{.Names}}' | grep -q "GymCRM"; then
    echo -e "${YELLOW}⚠️  No GymCRM services are currently running${NC}"
    exit 0
fi

echo -e "${BLUE}╔════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║${NC}  ${YELLOW}🛑 Stopping GymCRM Services${NC}           ${BLUE}║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════╝${NC}"
echo ""

# Show running services before stopping
echo -e "${YELLOW}📊 Currently running services:${NC}"
docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | grep -E "NAMES|GymCRM"
echo ""

echo -e "${BLUE}Using compose file: ${COMPOSE_FILE}${NC}"
echo ""

if [ "$REMOVE_VOLUMES" = true ]; then
    echo -e "${RED}⚠️  WARNING: This will remove all volumes and delete data!${NC}"
    read -p "Are you sure? (yes/no): " -r
    echo
    if [[ $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        echo -e "${YELLOW}Stopping services and removing volumes...${NC}"
        docker compose -f "$COMPOSE_FILE" down -v
        echo -e "${GREEN}✅ Services stopped and volumes removed${NC}"
    else
        echo -e "${BLUE}Cancelled${NC}"
        exit 0
    fi
else
    echo -e "${YELLOW}Stopping services (keeping volumes)...${NC}"
    docker compose -f "$COMPOSE_FILE" down
    echo -e "${GREEN}✅ Services stopped${NC}"
    echo -e "${BLUE}💡 Use -v flag to remove volumes and data${NC}"
fi