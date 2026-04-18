# Unity Cloud Testing

This project uses GitHub Actions plus GameCI for cloud checks.

## What Runs Immediately

The `repository-smoke` job checks that the Unity project files exist and that the main bootstrap script is structurally balanced.

This job does not need a Unity license.

## What Requires Unity Secrets

The `unity-webgl-build` job runs Unity in the cloud and builds WebGL. It is disabled until `UNITY_CI_ENABLED` is set to `true` in repository variables.

Required GitHub Actions secrets:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

For Unity Personal, GameCI currently expects a local Unity Hub license file. Their current activation docs say to activate Unity locally, locate the `.ulf` file, then store its contents in `UNITY_LICENSE`.

## Enable The Unity Build

1. Open GitHub repository settings.
2. Go to **Settings > Secrets and variables > Actions**.
3. Add the three secrets above.
4. Add a repository variable named `UNITY_CI_ENABLED` with value `true`.
5. Open **Actions > Unity Cloud Smoke**.
6. Click **Run workflow**.

Sources:

- GameCI activation docs: <https://game.ci/docs/github/activation/>
- GameCI builder docs: <https://game.ci/docs/github/builder/>
