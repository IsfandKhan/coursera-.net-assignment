# User Management API

A small ASP.NET Core minimal API for managing users in memory. The project includes CRUD endpoints, request validation, request logging middleware, and JWT authentication with a mock test user.

## Features

- `GET`, `POST`, `PUT`, and `DELETE` endpoints for `/users`
- In-memory user storage with `Dictionary<int, User>`
- Validation for empty names, empty emails, invalid email format, and duplicate emails
- Request logging middleware
- JWT authentication middleware
- Mock login endpoint that returns a valid bearer token
- `requests.http` file for quick manual testing

## Requirements

- .NET 10 SDK

## Run the project

```bash
dotnet restore
dotnet run
```

The API is configured to listen on:

```text
http://localhost:5000
```

## Authentication

Use the `/login` endpoint to get a JWT token.

Test credentials:

- Username: `testuser`
- Password: `Password123!`

Example request:

```http
POST /login
Content-Type: application/json

{
  "username": "testuser",
  "password": "Password123!"
}
```

Example response:

```json
{
  "accessToken": "<jwt-token>",
  "tokenType": "Bearer",
  "expiresIn": 3600
}
```

Send the token in the `Authorization` header for all `/users` endpoints:

```http
Authorization: Bearer <jwt-token>
```

## API Endpoints

### Public

- `GET /`
- `POST /login`

### Protected

- `GET /users`
- `GET /users/{id}`
- `POST /users`
- `PUT /users/{id}`
- `DELETE /users/{id}`

## Validation Rules

The API rejects invalid user data for create and update operations.

- `name` is required and cannot be blank
- `email` is required
- `email` must be in a valid email format
- `email` must be unique across users

Invalid requests return a validation error response.

## Sample User Payload

```json
{
  "name": "Charlie",
  "email": "charlie@example.com"
}
```

## Testing With REST Client

The repository includes [requests.http](/Users/isfandkhan/Desktop/coursera-.net-assignment/requests.http:1) for use with the VS Code REST Client extension or similar tools.

It contains:

- a health check request
- a login request that captures the JWT token
- authenticated CRUD requests for `/users`
- an invalid request example for validation testing

## Project Structure

- [Program.cs](/Users/isfandkhan/Desktop/coursera-.net-assignment/Program.cs:1): application setup, middleware, JWT auth, validation, and endpoints
- [requests.http](/Users/isfandkhan/Desktop/coursera-.net-assignment/requests.http:1): ready-to-run HTTP requests
- [coursera-.net-assignment.csproj](/Users/isfandkhan/Desktop/coursera-.net-assignment/coursera-.net-assignment.csproj:1): project file and package references

## Notes

- Data is stored in memory and resets when the app restarts.
- The JWT signing key and test credentials are hardcoded for assignment/demo purposes only.
- This is not intended for production use without persistent storage, secret management, and stronger auth flows.
