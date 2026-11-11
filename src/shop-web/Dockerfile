FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install --no-audit --no-fund
COPY . .
RUN npx ng build --configuration production

# Непривилегированный nginx слушает порт 8080.
FROM nginxinc/nginx-unprivileged:alpine AS final
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/shop-web/browser /usr/share/nginx/html
ENV CATALOG_API_URL=http://catalog-api:8080 \
    ORDERS_API_URL=http://orders-api:8080
EXPOSE 8080
