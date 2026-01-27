# Update Instructions

1. Bump the version in `package.json` using semver.
2. Review the code changes to identify features, fixes, and potential breaking changes.
3. Update the changelog.
4. Update `README.md` with any new instructions or documentation changes.
5. Stage the modified files:
   ```bash
   git add package.json CHANGELOG.md README.md
   ```
6. Commit the changes with a message indicating the new version:
   ```bash
   git commit -m "chore: bump version to X.Y.Z"
   ```
7. Push the changes to the remote repository:
   ```bash
   git push
   ```
