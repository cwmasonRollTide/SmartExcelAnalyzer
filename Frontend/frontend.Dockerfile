FROM node:20-alpine

WORKDIR /app

COPY package*.json ./

RUN npm install

COPY . .

RUN npm run build

EXPOSE 8080
EXPOSE 3000
EXPOSE 5001

CMD ["npm", "run", "dev"]