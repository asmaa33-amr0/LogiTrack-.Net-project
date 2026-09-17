# LogiTrack

A logistics and shipment management web application built with ASP.NET Core 8. LogiTrack lets customers request and track shipments, drivers update delivery progress from the road, and administrators manage the whole operation — drivers, warehouses, stations, and shipments — from a single dashboard.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [User Roles & Accounts](#user-roles--accounts)
- [API Reference](#api-reference)
- [Pricing Engine](#pricing-engine)
- [Project Structure](#project-structure)
- [Troubleshooting](#troubleshooting)

---

## Features

### Customer Portal
- Self-service registration and login
- Instant shipping quote before committing to a request
- Create shipment requests with pickup warehouse and destination station
- Track any shipment by tracking number
- View shipment history and cancel pending requests

### Driver Portal
- Login by username/email **or** by driver ID
- View all shipments assigned to the driver
- Update shipment status as the delivery progresses
- Log route checkpoints along the way

### Admin Dashboard
- Full CRUD for shipments, drivers, warehouses, and stations
- Assign drivers to shipments
- Create login accounts for drivers, or link existing accounts
- View all shipments belonging to a specific driver or warehouse
- Search across shipments, drivers, and warehouses

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8.0 (Web API) |
| Database | SQL Server + Entity Framework Core 8 |
| Authentication | ASP.NET Core Identity + JWT Bearer tokens |
| API Docs | Swagger / Swashbuckle |
| Frontend | Vanilla HTML, CSS, and JavaScript (served from `wwwroot`) |

---

## Architecture

LogiTrack is a single ASP.NET Core application that serves both the REST API and the static frontend.

- **API layer** — controllers under `/api/*`, secured with JWT bearer authentication and role-based authorization.
- **Data layer** — EF Core with `LT_DBContext`, code-first migrations applied automatically at startup.
- **Frontend** — three static pages (`index.html` for customers, `driver.html` for drivers, `admin.html` for admins). A shared `auth.js` handles token storage and authenticated requests, and routes each user to the portal that matches their role after login.

Authentication flow: the client posts credentials to `/api/Auth/Login`, receives a JWT containing the user's roles, stores it in `localStorage`, and sends it as a `Bearer` token on every subsequent request.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full edition)

### Setup

**1. Clone and enter the project**

```bash
git clone <repository-url>
cd LogiTrackWebV1.0
```

**2. Configure the database connection**

Update `ConnectionStrings:DefaultConnection` in `appsettings.json` to point at your SQL Server instance:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=Logi_Track;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

**3. Restore dependencies**

```bash
dotnet restore
```

**4. Run the application**

```bash
dotnet run
```

Migrations are applied and seed data is created automatically on first startup — no manual `dotnet ef database update` needed.

**5. Open the app**

Navigate to the URL shown in the console (e.g. `https://localhost:7xxx`). Interactive API docs are available at `/swagger`.

---

## Configuration

All settings live in `appsettings.json`.

### JWT

```json
"Jwt": {
  "Key": "<secret key — must be at least 32 bytes>",
  "Issuer": "LogiTrackAPI",
  "Audience": "LogiTrackClient",
  "DurationInMinutes": 120
}
```

> **Security note:** the key ships with a development placeholder. Replace it before deploying, and store it outside source control (user secrets, environment variables, or a key vault). The application refuses to start if the key is missing or shorter than 32 bytes.

### Seed Admin

```json
"DemoAuth": {
  "AdminUsername": "admin",
  "AdminEmail": "admin@logitrack.com",
  "AdminPassword": "LogiTrack@123"
}
```

These values create the initial administrator account on first run. Change them before any non-local deployment.

### Pricing (optional)

Pricing works out of the box with sensible defaults. To override, add a `Pricing` section:

```json
"Pricing": {
  "Currency": "EGP",
  "BaseFee": 20,
  "PricePerKg": 2.5,
  "PricePerKm": 0.5,
  "TodaySurchargePercent": 15,
  "SurchargeStepPercent": 5,
  "MaxSurchargeDays": 2
}
```

---

## User Roles & Accounts

The system seeds five roles: **Admin**, **Driver**, **Customer**, **Warehouse**, and **Station**.

| Role | How the account is created | Lands on |
|---|---|---|
| Admin | Seeded at startup from `DemoAuth` config | `admin.html` |
| Customer | Public self-registration on the login page | `index.html` |
| Driver | Created by an admin from the Drivers tab | `driver.html` |

**Default admin credentials:** `admin` / `LogiTrack@123`

> Public registration intentionally creates **Customer** accounts only. Privileged accounts cannot be self-assigned — driver logins are provisioned by an administrator.

### Creating a driver login

1. Sign in as an administrator and open the **Drivers** tab.
2. Create a new driver, or edit an existing one.
3. Fill in **New Login Username**, **New Login Email**, and **New Login Password**, then save.
4. The driver can now sign in on the main login page with those credentials, or with their numeric driver ID and password.

Passwords must satisfy the ASP.NET Core Identity defaults: at least 6 characters, including an uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character (e.g. `Driver@123`).

---

## API Reference

Base path: `/api`. All endpoints require a `Bearer` token unless marked public.

### Authentication — `/api/Auth`

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/Register` | Public | Register a new customer account |
| POST | `/Login` | Public | Authenticate and receive a JWT |

### Shipments — `/api/Shipments`

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/` | Authenticated | List all shipments |
| GET | `/search` | Authenticated | Search shipments |
| GET | `/{id}` | Authenticated | Get a shipment by ID |
| POST | `/` | Admin | Create a shipment |
| PUT | `/{id}` | Admin | Update a shipment |
| DELETE | `/{id}` | Admin | Delete a shipment |
| GET | `/warehouses-lookup` | Authenticated | Warehouse options for dropdowns |
| GET | `/drivers-lookup` | Authenticated | Driver options for dropdowns |

### Drivers — `/api/Drivers`

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/` | Admin | List all drivers |
| GET | `/{id}` | Admin | Get a driver by ID |
| GET | `/{id}/shipments` | Admin | Shipments assigned to a driver |
| GET | `/search` | Admin | Search drivers |
| GET | `/available-accounts` | Admin | Unlinked accounts available for linking |
| POST | `/` | Admin | Create a driver |
| PUT | `/{id}` | Admin | Update a driver |
| POST | `/{id}/account` | Admin | Create a new login for a driver |
| PUT | `/{id}/account` | Admin | Link an existing account to a driver |
| DELETE | `/{id}` | Admin | Delete a driver |

### Warehouses — `/api/Warehouses`

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/` | Authenticated | List all warehouses |
| GET | `/search` | Authenticated | Search warehouses |
| GET | `/{id}/shipments` | Authenticated | Shipments at a warehouse |
| POST | `/` | Admin | Create a warehouse |
| PUT | `/{id}` | Admin | Update a warehouse |
| DELETE | `/{id}` | Admin | Delete a warehouse |

### Stations — `/api/Stations`

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/` | Public | List all stations |
| GET | `/{id}` | Public | Get a station by ID |
| POST | `/` | Admin | Create a station |
| PUT | `/{id}` | Admin | Update a station |
| DELETE | `/{id}` | Admin | Delete a station |

### Customer Portal — `/api/CustomerUi`

Requires the **Customer** role.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/lookups` | Warehouses and stations for the request form |
| POST | `/quote` | Calculate a price quote |
| POST | `/requests` | Submit a shipment request |
| GET | `/requests` | List the customer's shipments |
| GET | `/requests/{id}` | Get one of the customer's shipments |
| GET | `/track/{trackingNumber}` | Track a shipment |
| PUT | `/requests/{id}/cancel` | Cancel a pending request |

### Driver Portal — `/api/DriverUi`

Requires the **Driver** or **Admin** role.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/driver/{driverId}` | Shipments assigned to the driver |
| GET | `/shipment/{id}` | Shipment details |
| PUT | `/shipment/{id}/status` | Update shipment status |
| POST | `/shipment/{id}/checkpoint` | Add a route checkpoint |
| GET | `/shipment/{id}/checkpoints` | List route checkpoints |
| GET | `/lookups` | Reference data for the driver UI |

---

## Pricing Engine

Quotes are calculated by `PricingService` from three components:

```
Total = BaseFee + (Weight × PricePerKg) + (Distance × PricePerKm) + DateSurcharge
```

The date surcharge rewards flexible delivery windows: same-day delivery adds 15%, the next day 10%, two days out 5%, and anything beyond three days carries no surcharge. Every value is configurable through the `Pricing` section described above.

---

## Project Structure

```
LogiTrackWebV1.0/
├── Controllers/          # API endpoints
│   ├── AuthController.cs
│   ├── ShipmentsController.cs
│   ├── DriversController.cs
│   ├── WarehousesController.cs
│   ├── StationsController.cs
│   ├── CustomerUiController.cs
│   └── DriverUiController.cs
├── Models/               # EF Core entities
├── DTOs/                 # Request and response contracts
├── Services/             # Pricing engine
├── Migrations/           # EF Core migrations
├── wwwroot/              # Frontend
│   ├── index.html        # Customer portal
│   ├── driver.html       # Driver portal
│   ├── admin.html        # Admin dashboard
│   ├── auth.js           # Shared auth and API helper
│   └── styles.css
├── LT_DBContext.cs       # Database context
├── DbInitializer.cs      # Migrations and seed data
└── Program.cs            # App configuration and startup
```

 
