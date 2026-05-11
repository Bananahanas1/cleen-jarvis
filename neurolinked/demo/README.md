# NeuroLinked V1.3 â€” Public Demo Server

Run a public, rate-limited, ephemeral-tenant demo of the NeuroLinked
brain at your own domain. Anyone can try it â€” nobody can abuse it.

## What this does

- Runs the brain in a Docker container with demo mode on
- Reverse-proxies through Caddy with automatic HTTPS (Let's Encrypt)
- Each visitor's session gets an ephemeral tenant (isolated memory)
- Idle tenants get reaped after 15 minutes (configurable)
- Rate-limits per IP at the edge (Caddy) and per token (app-level)

## Deploy

1. Point your DNS at the host:
   ```
   demo.neurolinked.ai  A  <host ip>
   ```

2. Edit `Caddyfile` â€” change `demo.neurolinked.ai` to your domain.

3. Build + run:
   ```bash
   cd demo
   docker-compose up -d
   ```

4. Check it's live:
   ```bash
   curl https://your-domain.com/api/jarvis/summary
   ```

## Configuration

Env vars (override in `docker-compose.yml` or `.env`):

- `NLKD_DEMO_IDLE_TIMEOUT_SEC` â€” how long an ephemeral tenant can sit
  idle before the reaper kills it (default: 900 = 15 min)
- `NLKD_DEMO_RATE_LIMIT_PER_MIN` â€” app-level rate limit (default: 30)
- `NLKD_DEMO_SCAN_INTERVAL_SEC` â€” reaper scan frequency (default: 300)

## Operational notes

- Demo container uses a 10,000-neuron brain â€” runs comfortably on a
  $12-24/mo DigitalOcean droplet.
- Volumes: `demo_brain_state` is the data volume; nuking it resets the
  public brain to zero.
- Backups: the demo brain is intentionally ephemeral. Don't back it up.
- Monitoring: `curl https://demo.neurolinked.ai/api/jarvis/summary` is
  the simplest health check. Hook it to UptimeRobot or similar.

## Security notes

- Demo mode restricts which endpoints are callable (see `demo_filters.py`
  in the server if shipped).
- Audit log + PII redaction are still active â€” even demo usage goes
  through the tamper-evident log.
- Never point this at a brain with real client data.

