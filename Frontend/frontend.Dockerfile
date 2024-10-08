# # FROM node:18 AS build
# # WORKDIR /app
# # COPY package*.json ./
# # RUN npm install
# # COPY . .
# # ARG BASE_API_URL
# # ENV VITE_BASE_API_URL=$BASE_API_URL
# # RUN npm run build

# # FROM nginx:stable-alpine
# # COPY --from=build /app/dist /usr/share/nginx/html
# # COPY nginx.conf /etc/nginx/conf.d/default.conf
# # CMD ["/]
# # EXPOSE 80
# # EXPOSE 3000

# FROM node:18-alpine
# WORKDIR /app
# COPY package*.json .
# RUN npm install
# COPY . .
# ARG BASE_API_URL
# ENV VITE_BASE_API_URL=$BASE_API_URL
# EXPOSE 3000
# EXPOSE 3001
# EXPOSE 80
# RUN npm run build
# CMD ["npm", "run","dev"]

# Use an official Node runtime as the base image
FROM node:20-alpine

# Set the working directory in the container
WORKDIR /app

# Copy package.json and package-lock.json
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy the rest of the application code
COPY . .

# Build the app
RUN npm run build

# Expose the port the app runs on
EXPOSE 3000

# Start the app
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]