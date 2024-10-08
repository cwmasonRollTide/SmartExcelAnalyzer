FROM node:18 AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
ARG BASE_API_URL
ENV VITE_BASE_API_URL=$BASE_API_URL
RUN npm run build

FROM nginx:stable-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]