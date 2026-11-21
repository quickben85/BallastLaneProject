# Check service health

As a reviewer, I can verify that the Tasks API responds before signing in.

Public `GET /api/health` returns `200 OK` with the service status and accepts an anonymous request. This is a liveness check for the HTTP host.
