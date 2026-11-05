FROM node:22-alpine AS build
WORKDIR /app

COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci

COPY src/frontend/ ./
RUN npm run build

FROM nginx:1.27-alpine AS runtime

ENV API_UPSTREAM=api:8080
ENV API_SCHEME=http
ENV API_HOST_HEADER=\$host

COPY src/frontend/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

EXPOSE 80

