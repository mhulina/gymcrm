#!/bin/bash

set -e

# Color codes
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${BLUE}🔐 Setting up User Secrets from .env.development${NC}"
echo ""

# Check if .env.development exists
if [ ! -f ".env.development" ]; then
    echo -e "${RED}❌ Error: .env.development not found!${NC}"
    echo "Please create .env.development file first."
    exit 1
fi

# Function to convert environment variable name to configuration path
# Example: ConnectionStrings__DefaultConnection -> ConnectionStrings:DefaultConnection
convert_to_config_path() {
    local var_name=$1
    echo "$var_name" | sed 's/__/:/g'
}

# Function to extract value from .env file
get_env_value() {
    local key=$1
    local value=$(grep "^${key}=" .env.development | cut -d'=' -f2- | xargs)
    echo "$value"
}

# Load all variables from .env.development
echo -e "${YELLOW}📖 Reading .env.development...${NC}"
echo ""

# Extract values
POSTGRES_USER=$(get_env_value "POSTGRES_USER")
POSTGRES_PASSWORD=$(get_env_value "POSTGRES_PASSWORD")
IDENTITY_DB_NAME=$(get_env_value "IDENTITY_DB_NAME")
SCHEDULING_DB_NAME=$(get_env_value "SCHEDULING_DB_NAME")
JWT_SECRET_KEY=$(get_env_value "JWT_SECRET_KEY")
JWT_ISSUER=$(get_env_value "JWT_ISSUER")
JWT_AUDIENCE=$(get_env_value "JWT_AUDIENCE")
JWT_EXPIRY_MINUTES=$(get_env_value "JWT_EXPIRY_MINUTES")
MAX_FAILED_ATTEMPTS=$(get_env_value "Security__MaxFailedLoginAttempts")
LOCKOUT_DURATION=$(get_env_value "Security__LockoutDurationMinutes")
AUTH_RATE_LIMIT=$(get_env_value "Security__AuthRateLimitPerMinute")
REGISTER_RATE_LIMIT=$(get_env_value "Security__RegisterRateLimitPerMinute")

# Verify critical values were found
if [ -z "$POSTGRES_PASSWORD" ] || [ -z "$JWT_SECRET_KEY" ]; then
    echo -e "${RED}❌ Error: Missing critical values in .env.development${NC}"
    echo "Required: POSTGRES_PASSWORD, JWT_SECRET_KEY"
    exit 1
fi

# ===============================
# IdentityAPI User Secrets
# ===============================
echo -e "${GREEN}🔐 Setting up IdentityAPI User Secrets...${NC}"

cd GymCRM.IdentityAPI

# Initialize if not already done
if ! grep -q "UserSecretsId" GymCRM.IdentityAPI.csproj; then
    echo "  Initializing User Secrets..."
    dotnet user-secrets init > /dev/null
fi

# Database Connection
CONNECTION_STRING="Host=localhost;Port=5432;Database=${IDENTITY_DB_NAME};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "$CONNECTION_STRING" > /dev/null
echo -e "  ${GREEN}✓${NC} ConnectionStrings:DefaultConnection"

# Authentication
dotnet user-secrets set "Authentication:SecretForKey" "$JWT_SECRET_KEY" > /dev/null
echo -e "  ${GREEN}✓${NC} Authentication:SecretForKey"

dotnet user-secrets set "Authentication:Issuer" "$JWT_ISSUER" > /dev/null
echo -e "  ${GREEN}✓${NC} Authentication:Issuer"

dotnet user-secrets set "Authentication:Audience" "$JWT_AUDIENCE" > /dev/null
echo -e "  ${GREEN}✓${NC} Authentication:Audience"

if [ -n "$JWT_EXPIRY_MINUTES" ]; then
    dotnet user-secrets set "Authentication:ExpiryMinutes" "$JWT_EXPIRY_MINUTES" > /dev/null
    echo -e "  ${GREEN}✓${NC} Authentication:ExpiryMinutes"
fi

# Security Settings
if [ -n "$MAX_FAILED_ATTEMPTS" ]; then
    dotnet user-secrets set "Security:MaxFailedLoginAttempts" "$MAX_FAILED_ATTEMPTS" > /dev/null
    echo -e "  ${GREEN}✓${NC} Security:MaxFailedLoginAttempts"
fi

if [ -n "$LOCKOUT_DURATION" ]; then
    dotnet user-secrets set "Security:LockoutDurationMinutes" "$LOCKOUT_DURATION" > /dev/null
    echo -e "  ${GREEN}✓${NC} Security:LockoutDurationMinutes"
fi

if [ -n "$AUTH_RATE_LIMIT" ]; then
    dotnet user-secrets set "Security:AuthRateLimitPerMinute" "$AUTH_RATE_LIMIT" > /dev/null
    echo -e "  ${GREEN}✓${NC} Security:AuthRateLimitPerMinute"
fi

if [ -n "$REGISTER_RATE_LIMIT" ]; then
    dotnet user-secrets set "Security:RegisterRateLimitPerMinute" "$REGISTER_RATE_LIMIT" > /dev/null
    echo -e "  ${GREEN}✓${NC} Security:RegisterRateLimitPerMinute"
fi

cd ..

echo ""

# ===============================
# SchedulingAPI User Secrets
# ===============================
echo -e "${GREEN}🔐 Setting up SchedulingAPI User Secrets...${NC}"

cd GymCRM.SchedulingAPI

# Initialize if not already done
if ! grep -q "UserSecretsId" GymCRM.SchedulingAPI.csproj; then
    echo "  Initializing User Secrets..."
    dotnet user-secrets init > /dev/null
fi

# Database Connection
CONNECTION_STRING="Host=localhost;Port=5432;Database=${SCHEDULING_DB_NAME};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "$CONNECTION_STRING" > /dev/null
echo -e "  ${GREEN}✓${NC} ConnectionStrings:DefaultConnection"

# Authentication
dotnet user-secrets set "Authentication:SecretForKey" "$JWT_SECRET_KEY" > /dev/null
echo -e "  ${GREEN}✓${NC} Authentication:SecretForKey"

dotnet user-secrets set "Authentication:Issuer" "$JWT_ISSUER" > /dev/null
echo -e "  ${GREEN}✓${NC} Authentication:Issuer"

dotnet user-secrets set "Authentication:Audience" "$JWT_AUDIENCE" > /dev/null
echo -e "  ${GREEN}✓${NC} Authentication:Audience"

# IdentityApiUrl - points to local debug instance
dotnet user-secrets set "IdentityApiUrl" "http://localhost:55080" > /dev/null
echo -e "  ${GREEN}✓${NC} IdentityApiUrl"

# Security Settings
if [ -n "$MAX_FAILED_ATTEMPTS" ]; then
    dotnet user-secrets set "Security:MaxFailedLoginAttempts" "$MAX_FAILED_ATTEMPTS" > /dev/null
    echo -e "  ${GREEN}✓${NC} Security:MaxFailedLoginAttempts"
fi

if [ -n "$LOCKOUT_DURATION" ]; then
    dotnet user-secrets set "Security:LockoutDurationMinutes" "$LOCKOUT_DURATION" > /dev/null
    echo -e "  ${GREEN}✓${NC} Security:LockoutDurationMinutes"
fi

cd ..

echo ""
echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║${NC}  ${GREEN}✅ User Secrets Setup Complete!${NC}                             ${BLUE}║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${YELLOW}📋 Next Steps:${NC}"
echo ""
echo "1. Start Docker database:"
echo -e "   ${GREEN}docker-compose -f docker-compose.debug.yml up -d postgres${NC}"
echo ""
echo "2. Configure Rider run configuration with:"
echo -e "   ${CYAN}ASPNETCORE_ENVIRONMENT=Development${NC}"
echo -e "   ${CYAN}ASPNETCORE_URLS=http://localhost:55080${NC} (for IdentityAPI)"
echo -e "   ${CYAN}ASPNETCORE_URLS=http://localhost:55085${NC} (for SchedulingAPI)"
echo ""
echo "3. Press F5 in Rider to debug!"
echo ""
echo -e "${BLUE}💡 To view your secrets:${NC}"
echo "   cd GymCRM.IdentityAPI && dotnet user-secrets list"
echo "   cd GymCRM.SchedulingAPI && dotnet user-secrets list"
echo ""
echo -e "${BLUE}💡 To clear secrets (if needed):${NC}"
echo "   cd GymCRM.IdentityAPI && dotnet user-secrets clear"
echo "   cd GymCRM.SchedulingAPI && dotnet user-secrets clear"
echo ""