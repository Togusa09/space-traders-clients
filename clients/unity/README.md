# SpaceTraders Unity Client

A Unity-based client for the SpaceTraders API.

## Requirements

- Unity 6000.4.6f1 or later.

## Getting Started

1. Open this folder (`clients/unity`) in the Unity Hub.
2. The project will initialize and import the necessary packages.
3. API client logic is located in `Assets/Scripts/API`.

## API Specification

The client uses the [SpaceTraders OpenAPI spec](https://spacetraders.io/SpaceTraders.json).

## GitHub Actions build pipeline

The repository includes a workflow at `.github/workflows/unity-build.yml` that:

- Runs Unity Edit Mode tests
- Builds player artifacts for Windows (`StandaloneWindows64`) and Linux (`StandaloneLinux64`)
- Uploads private GitHub Actions artifacts for authorized repository users to download

### Required repository secrets

Set these secrets in the repository before running the workflow:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

The workflow currently publishes private run artifacts only. Automatic GitHub Release creation is intentionally left for a future version.
