FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:24-alpine
WORKDIR /app
COPY --from=build /app/dist/task-manager/browser/ ./public/
COPY server.mjs ./
USER node
EXPOSE 8080
CMD ["node", "server.mjs"]
