# PocketBridge — Local bridge to Pocket for Open Coder (read-only first)

This module provides a local, read-only bridge to a Pocket-for-Open-Coder style app. It exposes a small API surface that Jarvis can query to list code files, read contents, and propose changes via the PendingApproval flow.

Usage (high level):
- Start a local OpenCode-like server (default URL http://localhost:6000) or adapt to your environment.
- Use PocketBridge from your code to query for code snippets or to import code through Jarvis PendingApproval.
- All write actions go through Jarvis PendingApproval; this bridge only reads and posts change requests.

Notes:
- This is a local utility intended for development; ensure the port is free and the calls stay within localhost.
