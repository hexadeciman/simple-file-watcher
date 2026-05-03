# FileStreamer

FileStreamer exposes a JSON file through:

- `GET /data`
- WebSocket `/ws?token=<supabase-jwt>`

The app reads a JSON file from disk, validates that it is valid JSON, and returns it as `application/json`. WebSocket clients receive the current JSON immediately after connecting and receive a new message every time the configured file changes.

## Prerequisites

- Docker
- Docker Compose
- A Supabase user access token for the allowed user
- Supabase project configured with asymmetric JWT signing keys

This app validates Supabase JWTs through the project's OpenID/JWKS metadata. Legacy HS256 Supabase JWT-secret tokens are not supported by this configuration.

## Configuration

Runtime configuration should be provided through `.env` and `compose.yaml`. The Dockerfile only builds the image.

The committed `.env` contains the current Compose configuration:

```bash
IMAGE_NAME=filestreamer
HOST_PORT=8080

HOST_JSON_DIR=/your/local/dir/path
CONTAINER_JSON_DIR=/data
JSON_FILE_PATH=/data/data.json

SUPABASE_URL=https://yourproject.supabase.co
ALLOWED_USER_ID=YOUR-SUPABASE-USER_ID
SUPABASE_AUDIENCE=authenticated
```

`HOST_JSON_DIR` is the directory on the Docker host. `JSON_FILE_PATH` is the full file path inside the container.

The app also has safe defaults in `appsettings.json`:

```json
{
  "JsonFile": {
    "Path": "/data/live.json"
  },
  "Supabase": {
    "Url": "",
    "AllowedUserId": "",
    "Audience": "authenticated"
  }
}
```

Compose maps the `.env` values to ASP.NET Core configuration variables:

```bash
JsonFile__Path=${JSON_FILE_PATH}
Supabase__Url=${SUPABASE_URL}
Supabase__AllowedUserId=${ALLOWED_USER_ID}
Supabase__Audience=${SUPABASE_AUDIENCE}
```

## Dockerfile Settings

The Dockerfile should not contain deployment-specific runtime settings. It uses the ASP.NET runtime image, exposes port `8080`, and starts `FileStreamer.dll`.

Do not put host file paths, Supabase URLs, or allowed user IDs in the Dockerfile. Set those in `.env` instead.

## Docker Compose

The included `compose.yaml` reads `.env`, maps the host JSON directory into the container, and passes the required app settings:

```yaml
services:
  filestreamer:
    image: ${IMAGE_NAME:-filestreamer}
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "${HOST_PORT:-8080}:8080"
    environment:
      ASPNETCORE_URLS: http://+:8080
      JsonFile__Path: ${JSON_FILE_PATH:-/data/live.json}
      Supabase__Url: ${SUPABASE_URL}
      Supabase__AllowedUserId: ${ALLOWED_USER_ID}
      Supabase__Audience: ${SUPABASE_AUDIENCE:-authenticated}
    volumes:
      - ${HOST_JSON_DIR:-./data}:${CONTAINER_JSON_DIR:-/data}:ro
```

Make sure the configured host file exists before starting. With the committed `.env`, the expected host file is:

```bash
/Users/your-user/Projects/vitals/data.json
```

Run:

```bash
docker compose up --build
```

If your host writes the JSON file somewhere else, update `.env`.

Example:

```bash
HOST_JSON_DIR=/host/path/export
CONTAINER_JSON_DIR=/json-source
JSON_FILE_PATH=/json-source/current.json
```

## CapRover Deployment

This repo includes a CapRover `captain-definition`:

```json
{
  "schemaVersion": 2,
  "dockerfilePath": "./Dockerfile"
}
```

GitHub Actions builds the Docker image in GitHub, pushes it to GitHub Container Registry, then asks CapRover to deploy that image. This keeps image builds off your server.

### CapRover App Setup

Create a CapRover app named `simple-file-watcher`, or use your preferred app name and set the same value in the GitHub secret `CAPROVER_APP`.

In the CapRover app settings, configure these environment variables:

```bash
ASPNETCORE_URLS=http://+:8080
JsonFile__Path=/data/data.json
Supabase__Url=https://yourproject.supabase.co
Supabase__AllowedUserId=YOUR-SUPABASE-USER-ID
Supabase__Audience=authenticated
```

Configure persistent storage or a host path mount so the JSON file exists inside the app container at the same path used by `JsonFile__Path`. For example, mount the host/export directory to `/data` and make sure `/data/data.json` exists.

The container listens on port `8080`, so configure the CapRover app container HTTP port as `8080`.

### GitHub Actions Setup

The workflow is in `.github/workflows/deploy.yml` and runs on pushes to `main` or manual `workflow_dispatch`.

Add these repository secrets in GitHub:

```bash
CAPROVER_SERVER=https://captain.apps.your-domain.com
CAPROVER_APP=simple-file-watcher
CAPROVER_APP_TOKEN=your-caprover-app-token
```

To get `CAPROVER_APP_TOKEN`, open the app in CapRover, go to the Deployment tab, enable the app token, and copy it.

The workflow pushes images to GitHub Container Registry using `GITHUB_TOKEN`. In GitHub, make sure Actions has package write permission:

- Repository Settings
- Actions
- General
- Workflow permissions
- Select `Read and write permissions`

If the CapRover app cannot pull from GHCR, either make the package public or configure `ghcr.io` as a private registry in CapRover with credentials that can read the package.

## REST Usage

```bash
curl http://localhost:8080/data \
  -H "Authorization: Bearer <supabase-jwt>"
```

Responses:

- `200 application/json` when the token is valid and the file contains valid JSON
- `401` when the token is missing or invalid
- `403` when the token is valid but the `sub` claim is not the configured allowed user id
- `500 application/problem+json` when the JSON file cannot be read or contains invalid JSON

## WebSocket Usage

Connect to:

```text
ws://localhost:8080/ws?token=<supabase-jwt>
```

Behavior:

- The token is validated with the same Supabase JWT settings as the REST endpoint.
- The first message is the current JSON file content.
- Every valid file change sends the new JSON content.
- If the file is unreadable or invalid JSON when read, the WebSocket receives an error object:

```json
{"error":"..."}
```

## Local Development

For local non-Docker runs, create `appsettings.Local.json`. The current local file mirrors `.env`, but is ignored by git:

```json
{
  "JsonFile": {
    "Path": "your-local-path"
  },
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "AllowedUserId": "your-allowed-user",
    "Audience": "authenticated"
  }
}
```

Run without Docker:

```bash
dotnet run
```

Then call:

```bash
curl http://localhost:5000/data \
  -H "Authorization: Bearer <supabase-jwt>"
```

The exact local port can vary depending on the ASP.NET Core launch settings and environment. Docker always exposes `8080` with the included configuration.
