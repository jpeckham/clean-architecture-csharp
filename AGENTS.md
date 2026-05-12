# Codex Operating Notes

## Docker Compose Is Required For App Changes

This repository's default end-to-end runtime is Docker Compose. For any change that touches `src/SocialApp.Api`, `src/SocialApp.Web`, infrastructure registration, Dockerfiles, HTTP contracts, appsettings, persistence mappings, or user-visible flows:

1. Run the normal relevant `dotnet test` commands.
2. Run `docker compose config` from the repository root.
3. Run `docker compose build` from the repository root.
4. When the request changes a user-facing flow, run `docker compose up -d` and smoke test the flow through:
   - API: `http://localhost:8080`
   - Web: `http://localhost:8081`
5. Report the Docker Compose result in the final answer.

Do not substitute only `dotnet run` local launch profiles for Docker Compose verification when the app or API/Web integration changes.
