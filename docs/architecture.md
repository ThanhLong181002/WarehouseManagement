# System Architecture

## Overview

WarehouseManagement consists of a web frontend, a backend API, and a SQL Server database.

## Components

### Frontend

`WarehouseManagement.Web`

Responsible for rendering the user interface and sending requests to the backend API.

### Backend

`WarehouseManagement.Api`

Responsible for authentication, API endpoints, validation, business rules, and database access.

### Database

Microsoft SQL Server

Stores material and warehouse data, stock transactions, inventory balances, and related records.

## Request Flow

1. The user interacts with the Blazor frontend.
2. The frontend sends an HTTP request to the backend API.
3. The API validates the request and applies the relevant business rules.
4. The API reads or updates SQL Server.
5. The API returns the response to the frontend.

## Security

Authentication uses JWT Bearer. Secrets and environment-specific settings must be configured outside source control.

## Notes

Update this document when the actual API routes, database schema, or deployment architecture changes.
