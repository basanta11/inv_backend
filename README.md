# 🧩 Inventory Management System with Supplier Integration

Track stock, calculate dynamic reorder points from demand stats, and place supplier orders automatically or on demand.  
Includes a **Vue dashboard** and a **.NET API** backed by **PostgreSQL**, with an in-memory Pub/Sub message broker and a mock Supplier API for asynchronous delivery simulation.

---

## 🚀 Quick Setup Guide

### 1️⃣ Backend Setup (.NET + PostgreSQL)

**From `/backend`:**

```bash
# restore dependencies
dotnet restore

#if needed

dotnet sln add Inventory.Api/ Inventory.Domain/ Inventory.App/ Inventory.Infrastructure/
dotnet add Inventory.Api reference ../Inventory.App ../Inventory.Domain ../Inventory.Infrastructure
dotnet add Inventory.App reference ../Inventory.Domain
dotnet add Inventory.Infrastructure reference ../Inventory.Domain

dotnet add Inventory.Infrastructure reference ../Inventory.Domain
dotnet add Inventory.Infrastructure reference ../Inventory.Domain

dotnet add Inventory.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add Inventory.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add Inventory.Infrastructure package Microsoft.EntityFrameworkCore.Design

# Tools
dotnet tool install --global dotnet-ef

dotnet add Inventory.App package AutoMapper
dotnet add Inventory.App package FluentValidation

dotnet add Inventory.Api package AutoMapper.Extensions.Microsoft.DependencyInjection

dotnet add Inventory.Api package Microsoft.Extensions.Http

# generate migration (ensure AppDbContext is in Inventory.Infrastructure)
dotnet ef migrations add InitialCreate -p Inventory.Infrastructure -s Inventory.Api

# apply migrations to PostgreSQL
dotnet ef database update -p Inventory.Infrastructure -s Inventory.Api

# run the API
dotnet run --project Inventory.Api
Default URL: http://localhost:5000

⚙️ Configuration (/backend/Inventory.Api/appsettings.Development.json)
json
Copy code
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=inventory_db;Username=postgres;Password=postgres"
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
Change database or frontend origin as needed.

To use SQLite for local testing:

json
Copy code
"ConnectionStrings": { "DefaultConnection": "Data Source=inventory.db" }
2️⃣ Frontend Setup (Vue 3 + Vite)
From /frontend:

bash
Copy code
# install dependencies
npm install

# (optional) install Tailwind fix
npm i -D tailwindcss @tailwindcss/postcss postcss autoprefixer
Vite Config (vite.config.ts)
ts
Copy code
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

export default defineConfig({
  plugins: [vue()],
  resolve: { alias: { '@': path.resolve(__dirname, './src') } }
})
TypeScript Config (tsconfig.app.json)
json
Copy code
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": { "@/*": ["src/*"] }
  }
}
Environment File (/frontend/.env)
ini
Copy code
VITE_API_BASE_URL=http://localhost:5000
Run Frontend:

bash
Copy code
npm run dev
Default URL: http://localhost:5173

3️⃣ (Optional) Seed Sample Data
Add a one-time seeding endpoint or startup code:

csharp
Copy code
// POST /api/dev/seed
if (!await db.Items.AnyAsync()) {
    // create ~15 items, demand stats, and sample supplier orders
    await db.SaveChangesAsync();
}
4️⃣ (Optional) Run via Docker Compose
Create /docker-compose.yml at repo root:

yaml
Copy code
services:
  db:
    image: postgres:16
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: inventory_db
    ports: ["5432:5432"]

  api:
    build: ./backend/Inventory.Api
    environment:
      ASPNETCORE_URLS: http://+:5000
      ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=inventory_db;Username=postgres;Password=postgres
      Cors__AllowedOrigins__0: http://localhost:5173
    depends_on: [db]
    ports: ["5000:5000"]

  web:
    build: ./frontend
    command: ["npm", "run", "dev", "--", "--host"]
    environment:
      VITE_API_BASE_URL: http://localhost:5000
    depends_on: [api]
    ports: ["5173:5173"]

volumes:
  db_data:
🔌 API Endpoints
🧱 Inventory
Method	Endpoint	Description	Body Example
GET	/api/items?q=&page=&pageSize=	List inventory items with pagination	–
POST	/api/items	Create new item	{ "sku": "SKU123", "name": "Widget", "stock": 50, "safetyStock": 5 }
PATCH	/api/items/{id}/reorder-threshold	Set manual reorder threshold (nullable int)	10 or null
PATCH	/api/items/{id}/force-reorder	Manually trigger supplier order	{ "quantity": 10, "deliveryDate": "2025-10-16" }

📈 Demand
Method	Endpoint	Description
GET	/api/demand/{itemId}?days=30	Get last N days of demand stats for item

📦 Orders
Method	Endpoint	Description
GET	/api/orders?page=&pageSize=	List supplier orders (Pending/Delivered)

🧪 Simulation & Development
Method	Endpoint	Description
POST	/api/simulate/demand	Simulate daily demand decay/increment
POST	/api/dev/seed	Seed sample data (items, demand stats, orders)

🔔 Supplier Webhooks
Method	Endpoint	Description
POST	/api/webhooks/supplier	Supplier posts order updates

🧠 What Each Service Does
🏭 Inventory Service (Backend API)
Built in .NET 8 with EF Core and PostgreSQL

Handles:

CRUD for items

Demand tracking

Dynamic reorder point calculation

Emits StockLowEvent when stock < threshold

Receives supplier webhooks to update order status

 nightly simulator for demand updates

🚚 Supplier Service (Mock)
Simple API that simulates supplier behavior:

Accepts incoming purchase orders

Waits a few seconds (async simulation)

Sends webhook back to Inventory Service confirming “Delivered”

💻 Frontend (Vue 3 + Bootstrap)
Pages:

Dashboard → overview of inventory and stock status

Inventory → list items, edit thresholds, trigger manual reorder

Item Detail → chart of last 30 days’ demand

Orders → view all supplier orders and statuses

Uses:

@tanstack/vue-query for async data caching

Pinia for state management

Axios for API calls

Bootstrap for UI styling

🪄 Background Services
InventoryMonitor: listens to StockLowEvent → creates supplier orders

DemandSimulator: nightly task updates demand stats and recomputes reorder points

PubSub: in-memory message broker (replaceable with RabbitMQ/Kafka later)

⚙️ Data Flow Summary
css
Copy code
Inventory.Api (EF + PostgreSQL)
     ↓ emits
[StockLowEvent] → InventoryMonitor → Supplier API
     ↑ webhook
Supplier.Mock → POST /api/webhooks/supplier
     ↓
Item stock updated + order marked delivered
🪵 Logging
Events logged in console or optionally to EventLogs table:

Event	Example Log
Stock low	Item 123 below reorder point (5 < 10)
Order created	Supplier order #A12 for Item 123 (qty: 20)
Webhook received	Order #A12 delivered on 2025-10-16
Simulator run	Demand updated for 15 items

📁 Project Structure
bash
Copy code
/backend
  ├─ Inventory.Domain/          (Entities, DTOs, Interfaces)
  ├─ Inventory.Infrastructure/  (EF Core DbContext, Config, Migrations)
  ├─ Inventory.App/             (Background Services, PubSub)
  └─ Inventory.Api/             (Controllers, DI, Webhooks)

/frontend
  ├─ src/
  │   ├─ views/        (Dashboard.vue, Inventory.vue, ItemDetail.vue, Orders.vue)
  │   ├─ services/     (inventory.service.ts, orders.service.ts, http.ts)
  │   ├─ router/       (App routes)
  │   └─ store/        (Pinia stores)
  └─ vite.config.ts
💡 Tips
CORS: must match frontend origin (http://localhost:5173)

EF Relation errors: check .ToTable("items") matches your DB

400 JSON conversion: ensure DTOs match backend signatures

PostCSS warning: install @tailwindcss/postcss

🧭 Tech Stack Summary
Layer	Tech
Backend	.NET 8, EF Core, PostgreSQL
Frontend	Vue 3, Vite, Bootstrap, Pinia, Axios, Vue Query
Events	In-memory Pub/Sub
Optional Infra	Docker Compose for full-stack setup