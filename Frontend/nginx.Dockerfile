FROM nginx:alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
EXPOSE 8080

CMD ["nginx", "-g", "daemon off;"]