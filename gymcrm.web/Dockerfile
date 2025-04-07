# Build Stage
FROM node:16 AS build

WORKDIR /app

# Copy package.json and package-lock.json first to install dependencies
COPY package*.json ./
RUN npm install

# Copy the rest of the app code
COPY . .

# Build the React app
RUN npm run build

# Production Stage
FROM node:16-slim

WORKDIR /app

# Install `serve` to serve the build
RUN npm install -g serve

# Copy the build directory from the build stage
COPY --from=build /app/build /app/build

# Expose port 3000 for the app
EXPOSE 3000

# Serve the app using `serve`
CMD ["serve", "-s", "build", "-l", "3000"]
