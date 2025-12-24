# Control Panel API Documentation

**Base URL:** `https://your-domain.com/api`
**Authentication:** All endpoints require Bearer token in `Authorization` header

---

## 🔐 Authentication

### Login
```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1...",
  "user": { "id": "guid", "name": "John Doe", "role": "Admin" }
}
```

---

## 📊 Dashboard

### Get Dashboard Summary
```http
GET /dashboard?from=2025-01-01&to=2025-12-31

Response:
{
  "nClients": 45,
  "activeOrders": 12,
  "servicesDelivered": 8,
  "revenue": 50000.00,
  "expense": 20000.00,
  "net": 30000.00,
  "lastServices": ["guid1", "guid2", "guid3"]
}
```

**Query Parameters:**
- `from` (required): Start date (YYYY-MM-DD)
- `to` (required): End date (YYYY-MM-DD)

---

## 👥 Clients

### List Clients
```http
GET /clients?search=acme&page=1&pageSize=20

Response:
{
  "items": [
    {
      "id": "guid",
      "businessName": "Acme Corp",
      "clientName": "John Doe",
      "phone": "+1234567890",
      "email": "contact@acme.com",
      "location": "New York",
      "projectCount": 5
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 45
}
```

**Query Parameters:**
- `search` (optional): Search term
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20, max: 200)

### Get Client Details
```http
GET /clients/{id}

Response:
{
  "id": "guid",
  "businessName": "Acme Corp",
  "clientName": "John Doe",
  "phone": "+1234567890",
  "email": "contact@acme.com",
  "location": "New York",
  "notesBrief": "Important client notes",
  "projectCount": 5,
  "openIssues": 2,
  "lastDelivery": "2025-06-15T10:00:00Z",
  "lastPayment": "2025-12-01",
  "projects": [
    { "id": "guid", "name": "Website", "status": "Activo" }
  ]
}
```

### Create Client
```http
POST /clients
Authorization: Bearer {token}
Role Required: Admin, Director

{
  "businessName": "Acme Corp",
  "clientName": "John Doe",
  "phone": "+1234567890",
  "email": "contact@acme.com",
  "location": "New York",
  "notesBrief": "VIP client"
}

Response: 201 Created
{ "id": "guid" }
```

### Update Client
```http
PUT /clients/{id}
Authorization: Bearer {token}
Role Required: Admin, Director

{
  "businessName": "Acme Corp",
  "clientName": "John Doe",
  "phone": "+1234567890",
  "email": "contact@acme.com",
  "location": "New York",
  "notesBrief": "Updated notes"
}

Response: 204 No Content
```

### Delete Client (Soft Delete)
```http
DELETE /clients/{id}
Authorization: Bearer {token}
Role Required: Admin, Director

Response: 204 No Content
```

---

## 🚀 Projects

### List Projects
```http
GET /projects?clientId=guid&status=Activo&page=1&pageSize=20

Response:
{
  "items": [
    {
      "id": "guid",
      "clientId": "guid",
      "clientName": "Acme Corp",
      "name": "E-commerce Website",
      "status": "Activo",
      "billingType": "Suscripción",
      "monthlyFee": 500.00,
      "oneOffFee": 5000.00,
      "siteUrl": "https://acme.com",
      "hasGeshkSubdomain": true,
      "domainController": "GESHK",
      "startedAt": "2025-01-15T10:00:00Z",
      "deliveredAt": "2025-06-15T10:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 12
}
```

**Query Parameters:**
- `clientId` (optional): Filter by client
- `status` (optional): Filter by status (Activo, En Proceso, En Pausa, Entregado, Cerrado)
- `billingType` (optional): Filter by billing (Suscripción, Único)
- `q` (optional): Search in name, URL, subdomain, client name
- `page`, `pageSize`: Pagination

### Get Project Details
```http
GET /projects/{id}

Response:
{
  "id": "guid",
  "clientId": "guid",
  "clientName": "Acme Corp",
  "name": "E-commerce Website",
  "status": "Activo",
  "billingType": "Suscripción",
  "monthlyFee": 500.00,
  "oneOffFee": 5000.00,
  "currency": "USD",
  "domain": {
    "siteUrl": "https://acme.com",
    "hasGeshkSubdomain": true,
    "subdomain": "acme",
    "domainController": "GESHK",
    "registrar": "GoDaddy",
    "hostingProvider": "AWS",
    "nameservers": "ns1.example.com, ns2.example.com"
  },
  "dates": {
    "startedAt": "2025-01-15T10:00:00Z",
    "dueAt": "2025-06-01T10:00:00Z",
    "deliveredAt": "2025-06-15T10:00:00Z"
  },
  "owner": {
    "userId": "guid",
    "name": "Jane Smith"
  },
  "notesRecent": [
    {
      "id": "guid",
      "scopeType": "Proyecto",
      "scopeId": "guid",
      "content": "Client requested changes",
      "isPinned": false,
      "createdByName": "John Admin",
      "createdAt": "2025-12-20T10:00:00Z",
      "editedAt": null,
      "editedByName": null
    }
  ],
  "issuesOpen": [
    {
      "id": "guid",
      "projectId": "guid",
      "title": "Login bug",
      "description": "Users can't login",
      "severity": "High",
      "status": "Open",
      "createdByName": "John Admin",
      "createdAt": "2025-12-20T10:00:00Z",
      "resolvedAt": null
    }
  ]
}
```

### Create Project
```http
POST /projects
Authorization: Bearer {token}
Role Required: Admin, Director

{
  "clientId": "guid",
  "name": "E-commerce Website",
  "status": "En Proceso",
  "billingType": "Suscripción",
  "monthlyFee": 500.00,
  "oneOffFee": 5000.00,
  "currency": "USD",
  "siteUrl": "https://acme.com",
  "hasGeshkSubdomain": true,
  "subdomain": "acme",
  "domainController": "GESHK",
  "registrar": "GoDaddy",
  "hostingProvider": "AWS",
  "nameservers": "ns1.example.com, ns2.example.com",
  "startedAt": "2025-01-15T10:00:00Z",
  "dueAt": "2025-06-01T10:00:00Z",
  "ownerUserId": "guid"
}

Response: 201 Created
{ "id": "guid" }
```

### Update Project
```http
PUT /projects/{id}
Authorization: Bearer {token}
Role Required: Admin, Director

{
  "name": "E-commerce Website",
  "status": "Activo",
  "billingType": "Suscripción",
  "monthlyFee": 500.00,
  "oneOffFee": 5000.00,
  "currency": "USD",
  "siteUrl": "https://acme.com",
  "hasGeshkSubdomain": true,
  "subdomain": "acme",
  "domainController": "GESHK",
  "registrar": "GoDaddy",
  "hostingProvider": "AWS",
  "nameservers": "ns1.example.com, ns2.example.com",
  "dueAt": "2025-06-01T10:00:00Z",
  "ownerUserId": "guid"
}

Response: 204 No Content
```

### Delete Project (Soft Delete)
```http
DELETE /projects/{id}
Authorization: Bearer {token}
Role Required: Admin, Director

Response: 204 No Content
```

---

## 🐛 Issues

### List Issues by Project
```http
GET /projects/{projectId}/issues?status=Open&severity=High&page=1&pageSize=20

Response:
{
  "items": [
    {
      "id": "guid",
      "projectId": "guid",
      "title": "Login bug",
      "description": "Users can't login with email",
      "severity": "High",
      "status": "Open",
      "createdByName": "John Admin",
      "createdAt": "2025-12-20T10:00:00Z",
      "resolvedAt": null
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 5
}
```

**Query Parameters:**
- `status` (optional): Open, In Progress, Resolved, Won't Fix
- `severity` (optional): Low, Med, High
- `q` (optional): Search in title/description

### Get Issue Details
```http
GET /issues/{id}

Response:
{
  "id": "guid",
  "projectId": "guid",
  "title": "Login bug",
  "description": "Users can't login with email",
  "severity": "High",
  "status": "Open",
  "createdByName": "John Admin",
  "createdAt": "2025-12-20T10:00:00Z",
  "resolvedAt": null
}
```

### Create Issue
```http
POST /projects/{projectId}/issues
Authorization: Bearer {token}
Role Required: Admin, Director, Operativo

{
  "title": "Login bug",
  "description": "Users can't login with email",
  "severity": "High"
}

Response: 201 Created
{ "id": "guid" }
```

**Severity Values:** Low, Med, High (case insensitive, auto-normalized)

### Update Issue
```http
PUT /issues/{id}
Authorization: Bearer {token}
Role Required: Admin, Director, Operativo

{
  "title": "Login bug - Fixed",
  "description": "Updated description",
  "severity": "Med",
  "status": "Resolved"
}

Response: 204 No Content
```

**Status Values:** Open, In Progress, Resolved, Won't Fix

### Change Issue Status
```http
PATCH /issues/{id}/status
Authorization: Bearer {token}
Role Required: Admin, Director, Operativo

{
  "status": "In Progress"
}

Response: 204 No Content
```

### Resolve Issue (Quick Action)
```http
PATCH /issues/{id}/resolve
Authorization: Bearer {token}
Role Required: Admin, Director, Operativo

Response: 204 No Content
```

### Delete Issue (Hard Delete)
```http
DELETE /issues/{id}
Authorization: Bearer {token}
Role Required: Admin, Director

Response: 204 No Content
```

---

## 💰 Finance

### Get Finance Summary
```http
GET /finance/summary?from=2025-01-01&to=2025-12-31&groupBy=month

Response:
{
  "balance": 30000.00,
  "income": 50000.00,
  "expense": 20000.00,
  "growthPctVsPrevPeriod": 0.15,
  "series": [
    {
      "key": "2025-01",
      "income": 5000.00,
      "expense": 2000.00,
      "net": 3000.00
    },
    {
      "key": "2025-02",
      "income": 6000.00,
      "expense": 2500.00,
      "net": 3500.00
    }
  ]
}
```

**Query Parameters:**
- `from` (required): Start date (YYYY-MM-DD)
- `to` (required): End date (YYYY-MM-DD)
- `groupBy` (optional): `month` or `category`

### List Transactions
```http
GET /finance/transactions?type=Ingreso&from=2025-01-01&to=2025-12-31&page=1&pageSize=20

Response:
{
  "items": [
    {
      "id": "guid",
      "date": "2025-12-20",
      "type": "Ingreso",
      "category": "Mensualidad",
      "concept": "Monthly subscription - Acme Corp",
      "amount": 500.00,
      "currency": "USD",
      "paymentMethod": "Transferencia",
      "clientId": "guid",
      "projectId": "guid",
      "tags": ["recurring", "subscription"]
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 150
}
```

**Query Parameters:**
- `type` (optional): Ingreso, Egreso
- `category` (optional): Filter by category
- `clientId` (optional): Filter by client
- `projectId` (optional): Filter by project
- `method` (optional): Payment method
- `min`, `max` (optional): Amount range
- `from`, `to` (optional): Date range
- `tag` (optional): Filter by tag
- `q` (optional): Search in concept

### Get Transaction
```http
GET /finance/transactions/{id}

Response:
{
  "id": "guid",
  "date": "2025-12-20",
  "type": "Ingreso",
  "category": "Mensualidad",
  "concept": "Monthly subscription - Acme Corp",
  "amount": 500.00,
  "currency": "USD",
  "paymentMethod": "Transferencia",
  "clientId": "guid",
  "projectId": "guid",
  "tags": ["recurring", "subscription"]
}
```

### Create Transaction
```http
POST /finance/transactions
Authorization: Bearer {token}
Role Required: Admin, Finanzas

{
  "date": "2025-12-20",
  "type": "Ingreso",
  "category": "Mensualidad",
  "concept": "Monthly subscription - Acme Corp",
  "amount": 500.00,
  "currency": "USD",
  "paymentMethod": "Transferencia",
  "clientId": "guid",
  "projectId": "guid",
  "tags": ["recurring", "subscription"]
}

Response: 201 Created
{ "id": "guid" }
```

**Type Values:** Ingreso, Egreso

### Update Transaction
```http
PUT /finance/transactions/{id}
Authorization: Bearer {token}
Role Required: Admin, Finanzas

{
  "date": "2025-12-20",
  "type": "Ingreso",
  "category": "Mensualidad",
  "concept": "Updated concept",
  "amount": 500.00,
  "currency": "USD",
  "paymentMethod": "Transferencia",
  "clientId": "guid",
  "projectId": "guid",
  "tags": ["recurring"]
}

Response: 204 No Content
```

### Delete Transaction (Soft Delete)
```http
DELETE /finance/transactions/{id}
Authorization: Bearer {token}
Role Required: Admin, Finanzas

{
  "reason": "Duplicate entry"
}

Response: 204 No Content
```

### Get Categories
```http
GET /finance/categories

Response: 200 OK
[
  "Mensualidad",
  "Venta",
  "Mantenimiento",
  "Soporte",
  "Compra Insumo",
  "Servicios Terceros",
  "Transporte"
]
```

---

## 📈 Economics (Project Planning)

### List Economic Plans for Project
```http
GET /projects/{id}/economics/plan

Response:
[
  {
    "id": "guid",
    "projectId": "guid",
    "scenarioName": "Conservative Estimate",
    "plannedOneOffRevenue": 5000.00,
    "plannedMonthlyRevenue": 500.00,
    "plannedInternalHours": 160,
    "hourlyRate": 50.00,
    "plannedInfraMonthly": 100.00,
    "plannedThirdPartyMonthly": 50.00,
    "plannedOneOffCosts": 1000.00,
    "plannedMonthlyCost": 200.00,
    "plannedMargin": 4300.00,
    "notes": "Based on similar projects",
    "createdAt": "2025-12-20T10:00:00Z",
    "createdBy": "guid"
  }
]
```

### Create Economic Plan
```http
POST /projects/{id}/economics/plan
Authorization: Bearer {token}
Role Required: Admin, Director

{
  "scenarioName": "Conservative Estimate",
  "plannedOneOffRevenue": 5000.00,
  "plannedMonthlyRevenue": 500.00,
  "plannedInternalHours": 160,
  "hourlyRate": 50.00,
  "plannedInfraMonthly": 100.00,
  "plannedThirdPartyMonthly": 50.00,
  "plannedOneOffCosts": 1000.00,
  "notes": "Based on similar projects"
}

Response: 201 Created
{ "id": "guid" }
```

### Compare Planned vs Actual Margin
```http
GET /finance/margin/project/{id}?from=2025-01-01&to=2025-12-31

Response:
{
  "planned": {
    "id": "guid",
    "projectId": "guid",
    "scenarioName": "Conservative Estimate",
    "plannedOneOffRevenue": 5000.00,
    "plannedMonthlyRevenue": 500.00,
    "plannedMonthlyCost": 200.00,
    "plannedMargin": 4300.00,
    ...
  },
  "actual": {
    "oneOffRevenue": 5500.00,
    "monthlyRevenue": 0.00,
    "monthlyCost": 2100.00,
    "oneOffCost": 0.00
  },
  "variance": {
    "oneOffRevenue": 500.00,
    "monthlyRevenue": -500.00,
    "monthlyCost": 1900.00,
    "oneOffCost": -1000.00
  }
}
```

---

## 🔍 Search

### Global Search
```http
GET /search?q=acme

Response:
[
  {
    "type": "Client",
    "id": "guid",
    "title": "Acme Corp",
    "subtitle": "John Doe",
    "extra": null
  },
  {
    "type": "Project",
    "id": "guid",
    "title": "E-commerce Website",
    "subtitle": "Acme Corp",
    "extra": "https://acme.com"
  },
  {
    "type": "Meeting",
    "id": "guid",
    "title": "Reunión · E-commerce Website",
    "subtitle": "2025-12-25 14:00 UTC",
    "extra": "Zoom"
  }
]
```

**Search Types:** Client, Project, Meeting

---

## 📝 Common Response Formats

### Paginated Response
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "total": 150
}
```

### Error Response
```json
{
  "message": "Error description"
}
```

### HTTP Status Codes
- `200 OK` - Success with response body
- `201 Created` - Resource created successfully
- `204 No Content` - Success without response body
- `400 Bad Request` - Validation error
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found

---

## 🔑 User Roles

- **Admin** - Full access to all endpoints
- **Director** - Can create/edit/delete clients, projects, issues
- **Operativo** - Can create/edit issues and notes
- **Finanzas** - Can manage transactions
- **Consulta** - Read-only access

---

## 📌 Important Notes

1. **Dates**: Use ISO 8601 format
   - DateTime: `2025-12-20T10:00:00Z`
   - Date only: `2025-12-20`

2. **Pagination**: Default page size is 20, max is 200

3. **Soft Deletes**: Most resources use soft delete (IsDeleted flag)

4. **Required Headers**:
   ```
   Authorization: Bearer {token}
   Content-Type: application/json
   ```

5. **Case Sensitivity**: Status and type values are case-insensitive and auto-normalized

6. **Currency**: Default is USD if not specified
