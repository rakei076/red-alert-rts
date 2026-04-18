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

## If Your Unity Account Uses Google Login

If you sign in to Unity with Google, do not put your Google password into GitHub Actions.

Use one of these routes instead:

- Preferred: use Unity Build Automation in Unity Cloud. Sign in with Google in the browser, connect this GitHub repository, and let Unity's own service run the build.
- Alternative: set a separate Unity ID password in your Unity account settings, then use that Unity ID email/password only for GameCI secrets.

For this project, the preferred route is Unity Build Automation because it matches Google login better and avoids storing a Google password in GitHub.

Suggested Unity Cloud setup:

1. Open <https://cloud.unity.com/>.
2. Sign in with Google.
3. Create or select a Unity project.
4. Open **DevOps > Build Automation**.
5. Connect this public GitHub repository: `https://github.com/rakei076/red-alert-rts`.
6. Select branch `main`.
7. Select Unity version `2022.3 LTS` or newer.
8. Add a WebGL build target first.
9. Run the build.
10. If it fails, copy the first compile error from the build log.

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
- Unity Build Automation docs: <https://docs.unity.com/ugs/manual/devops/manual/build-automation/get-started-with-build-automation/connect-your-version-control-system>
