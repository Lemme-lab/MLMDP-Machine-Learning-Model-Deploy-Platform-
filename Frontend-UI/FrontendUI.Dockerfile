# Stage 1: Build the Angular app
FROM node:20.17 as build-stage

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build --prod

# Stage 2: Serve the Angular app using Nginx
FROM nginx:alpine

# Remove default Nginx static files
RUN rm -rf /usr/share/nginx/html/*

# Copy the built Angular app from the previous stage to Nginx's web directory
COPY --from=build-stage /app/dist/frontend-ui /usr/share/nginx/html/

# Move the files from the 'browser' directory to the root directory
RUN mv /usr/share/nginx/html/browser/* /usr/share/nginx/html/ && rmdir /usr/share/nginx/html/browser

# Copy custom Nginx configuration if needed
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80 for the application
EXPOSE 80

# Start Nginx when the container launches
CMD ["nginx", "-g", "daemon off;"]
