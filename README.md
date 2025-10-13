# Inventory Management System with Supplier Integration

## Overview

A comprehensive inventory management system built with .NET 8 that monitors stock levels, predicts demand-based reorder points, and automates supplier interactions through webhooks. The system features real-time stock monitoring, automatic reorder triggers, and demand forecasting capabilities.

## Architecture & Design

### System Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   Backend API   │    │   Database      │
│   (Vue)       │◄──►│   (.NET 8)      │◄──►│   (Postgres)      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌─────────────────┐
                       │  Message Queue  │
                       │  (In-Memory)    │
                       └─────────────────┘
                              │
                              ▼
                       ┌─────────────────┐
                       │ Background Jobs │
                       │ & Services      │
                       └─────────────────┘
```

### Project Structure

The solution follows Clean Architecture principles with clear separation of concerns:

- **`Inventory.Domain`** - Core business entities and interfaces
- **`Inventory.Infrastructure`** - Data persistence and external services
- **`Inventory.App`** - Application services and business logic
- **`Inventory.Api`** - REST API controllers and web configuration

## Technical Stack

- **Backend**: .NET 8 (ASP.NET Core Web API)
- **Database**: SQLite with Entity Framework Core
- **Message Broker**: In-memory pub/sub system (production-ready for RabbitMQ)
- **Authentication**: None (development setup)
- **Documentation**: Swagger/OpenAPI

## Key Features

### 1. Inventory Management
- **Stock Tracking**: Real-time monitoring of item stock levels
- **Dynamic Reorder Points**: Automatic calculation based on demand forecasting
- **Manual Overrides**: Admin capability to set custom reorder thresholds
- **Stock Status**: Visual indicators (OK/Low) based on current stock vs reorder points

### 2. Demand Forecasting
- **Algorithm**: `Reorder Point = (Avg Daily Demand × Lead Time) + Safety Stock`
- **Rolling Statistics**: Maintains 30-day demand history per item
- **Automatic Updates**: Daily recalculation of reorder points
- **Demand Simulation**: Background service simulates realistic demand patterns

### 3. Supplier Integration
- **Order Placement**: Automated supplier order creation when stock is low
- **Webhook Integration**: Asynchronous order confirmation handling
- **Status Tracking**: Real-time order status updates (Pending/Confirmed/Failed)
- **Inventory Updates**: Automatic stock replenishment on confirmed orders

### 4. Real-time Monitoring
- **Background Services**: Continuous stock level monitoring
- **Event-Driven**: Pub/sub messaging for low stock alerts
- **Automated Triggers**: Instant reorder when stock drops below threshold

## API Endpoints

### Items Management
```http
GET    /api/items                    # List all items with status
POST   /api/items                    # Add new item
PUT    /api/items/{id}               # Update item details
GET    /api/items/{id}               # Get specific item
PATCH  /api/items/{id}/reorder-threshold  # Set manual reorder point
PATCH  /api/items/{id}/force-reorder      # Force reorder trigger
GET    /api/items/{id}/demand             # Get 30-day demand history
PATCH  /api/items/{id}/recompute          # Recalculate reorder point
```

### Supplier Operations
```http
POST   /supplier/place-order         # Place order with supplier
POST   /webhook/order-confirmation   # Webhook for order status updates
```

## Data Models

### Core Entities

#### Item
```csharp
public class Item {
    public Guid Id { get; set; }
    public string Sku { get; set; }           // Stock Keeping Unit
    public string Name { get; set; }          // Item name
    public int Stock { get; set; }            // Current stock level
    public int LeadTimeDays { get; set; }     // Supplier lead time
    public int SafetyStock { get; set; }      // Minimum safety buffer
    public int? ManualReorderPoint { get; set; }  // Manual override
    public int ComputedReorderPoint { get; set; } // Auto-calculated
}
```

#### DemandStat
```csharp
public class DemandStat {
    public long Id { get; set; }
    public Guid ItemId { get; set; }
    public DateTime Day { get; set; }         // Daily demand tracking
    public int Quantity { get; set; }         // Demand quantity
}
```

#### SupplierOrder
```csharp
public class SupplierOrder {
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
    public DateTime RequestedDeliveryDate { get; set; }
    public OrderStatus Status { get; set; }   // Pending/Confirmed/Failed
    public string? SupplierRef { get; set; }  // Supplier reference number
}
```

## Business Logic Flow

### 1. Stock Monitoring Flow
```
Stock Update → Check Reorder Point → Below Threshold? → Publish Event → Auto-Order
```

### 2. Demand Forecasting Flow
```
Daily Demand Data → Calculate 30-Day Average → Apply Lead Time → Add Safety Stock → Update Reorder Point
```

### 3. Supplier Integration Flow
```
Low Stock Event → Create Supplier Order → Send to Supplier API → Receive Webhook → Update Stock
```

## Background Services

### InventoryMonitor
- **Purpose**: Continuously monitors stock levels every 10 seconds
- **Action**: Publishes `StockLowEvent` when stock drops below reorder point
- **Impact**: Triggers automatic supplier orders

### DemandSimulator
- **Purpose**: Simulates realistic demand patterns for testing
- **Schedule**: Daily updates with random demand increments
- **Data**: Generates 30-day rolling demand statistics

## Setup Instructions

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- SQLite (included with .NET)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd backend
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Apply database migrations**
   ```bash
   dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.Api
   ```

4. **Run the application**
   ```bash
   dotnet run --project Inventory.Api
   ```

5. **Access the API**
   - API: `https://localhost:5001`
   - Swagger UI: `https://localhost:5001/swagger`

### Seed Data

The application automatically seeds with sample items:
- **Widget A** (SKU-001): 12 units, 5 safety stock, 2-day lead time
- **Widget B** (SKU-002): 3 units, 6 safety stock, 4-day lead time

## Configuration

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=inventory.db"
  }
}
//Need to updaet postgres
{
    
}
```

### CORS Policy
Configured to allow all origins for development (update for production):
```csharp
policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
```

## Key Algorithms

### Reorder Point Calculation
```csharp
var avgDailyDemand = await GetLast30DaysAverage(itemId);
var reorderPoint = (avgDailyDemand * leadTimeDays) + safetyStock;
```

### Stock Status Determination
```csharp
var threshold = item.ManualReorderPoint ?? item.ComputedReorderPoint;
var status = (threshold > 0 && item.Stock < threshold) ? "Low" : "OK";
```

## Testing & Development

### Sample API Calls

1. **Add New Item**
   ```json
   POST /api/items
   {
     "sku": "SKU-003",
     "name": "Widget C",
     "stock": 20,
     "leadTimeDays": 3,
     "safetyStock": 8
   }
   ```

2. **Set Manual Reorder Threshold**
   ```json
   PATCH /api/items/{id}/reorder-threshold
   {
     "reorderPoint": 15,
     "safetyStock": 10
   }
   ```

3. **Force Reorder**
   ```json
   PATCH /api/items/{id}/force-reorder
   Content-Type: application/json
   
   25
   ```

## Future Enhancements

### Planned Features
- **React Frontend**: Dashboard with real-time charts and inventory management
- **Docker Support**: Containerized deployment with docker-compose
- **RabbitMQ Integration**: Production-grade message queuing
- **PostgreSQL**: Enterprise database support
- **Authentication**: JWT-based security
- **Multi-tenant**: Support for multiple organizations
- **Advanced Analytics**: ML-based demand forecasting
- **Mobile App**: React Native mobile interface

### Production Considerations
- Replace SQLite with PostgreSQL/SQL Server
- Implement proper authentication and authorization
- Add comprehensive logging and monitoring
- Set up CI/CD pipelines
- Configure environment-specific settings
- Add unit and integration tests
- Implement proper error handling and validation

## Architecture Benefits

### Scalability
- **Clean Architecture**: Easy to extend and modify
- **Dependency Injection**: Loosely coupled components
- **Background Services**: Non-blocking operations
- **Event-Driven**: Responsive to real-time changes

### Maintainability
- **Separation of Concerns**: Clear module boundaries
- **SOLID Principles**: Well-structured codebase
- **Entity Framework**: Database abstraction
- **Swagger Documentation**: Self-documenting API

### Reliability
- **Transactional Operations**: Data consistency
- **Error Handling**: Graceful failure management
- **Idempotent Operations**: Safe retry mechanisms
- **Monitoring**: Real-time system health

## Contributing

1. Fork the repository
2. Create a feature branch
3. Implement changes with tests
4. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.