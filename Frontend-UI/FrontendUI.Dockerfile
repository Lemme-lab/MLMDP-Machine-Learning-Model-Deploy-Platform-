# Stage 1: Build the Angular app
FROM node:20.17 as build-stage

# Set the working directory inside the container
WORKDIR /app

# Copy the package.json and package-lock.json files
COPY package*.json ./

# Install the dependencies
RUN npm install

# Copy the rest of the application files
COPY . .

# Build the Angular app in production mode
RUN npm run build --prod

# Stage 2: Serve the Angular app using Nginx
FROM nginx:alpine

# Remove default Nginx static files
RUN rm -rf /usr/share/nginx/html/*

# Copy the built Angular app from the previous stage to Nginx's web directory
COPY --from=build-stage /app/dist/frontend-ui /usr/share/nginx/html

# Expose port 80 for the application
EXPOSE 80

# Start Nginx when the container launches
CMD ["nginx", "-g", "daemon off;"]
