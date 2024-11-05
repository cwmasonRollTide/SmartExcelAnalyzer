FROM nginx:alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 81

CMD ["nginx", "-g", "daemon off;"]