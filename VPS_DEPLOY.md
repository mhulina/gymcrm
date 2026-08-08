# Deploying to a VPS (no domain, IP only)

Runs the `docker-compose.prod.yml` stack (Postgres + API + static nginx frontend + nginx
reverse proxy) on a small cloud VPS, reachable at the droplet's public IP over plain HTTP. No
domain, no TLS - add both later if you ever want them (`nginx/nginx.prod.conf` already has a
ready-to-uncomment HTTPS block for that).

The droplet never builds anything itself - the `api` and `webapp` images are built by GitLab
CI (`.gitlab-ci.yml`'s `docker-build` job, running on your own PC as the runner) and pushed to
**DigitalOcean Container Registry (DOCR)**. The droplet just pulls the pre-built images and runs
them, which is why a 1GB RAM droplet is enough (a fresh `dotnet build` + `npm run build` on the
droplet itself would be a stretch on that little RAM - this sidesteps that entirely).

Both images share **one** DOCR repository (`gymcrm`), distinguished by tag prefix
(`gymcrm:api-latest` / `gymcrm:webapp-latest`) rather than being two separate repositories -
DOCR's free Starter tier only includes 1 repository (500MiB storage), and this fits both images
into it. If that's ever too tight, the Basic tier ($5/mo, 5 repos/5GiB) removes the need for the
shared-repo trick.

**Cost**: a 1GB RAM / 1 vCPU droplet is ~$6/mo (~€5.50) on DigitalOcean, DOCR Starter tier is
free - comfortably under €10/month with nothing else to pay for (no domain, no managed DB).

## 1. Create the droplet

- DigitalOcean → Create → Droplets.
- Image: **Ubuntu 24.04 LTS** (or whatever the current LTS is).
- Size: **Basic, 1GB RAM / 1 vCPU / 25GB SSD** (~$6/mo).
- Authentication: add your SSH key (recommended over a password).
- Create it, note the public IPv4 address shown on the droplet's dashboard - that's `<DROPLET_IP>`
  for every step below.

(Hetzner's CX22 is a cheaper equivalent if you'd rather use that - same steps from here on, just
Ubuntu + Docker either way.)

## 2. Firewall

Only SSH and HTTP need to be reachable (no HTTPS yet, since there's no domain for a cert).

- Easiest: DigitalOcean → Networking → Firewalls → Create Firewall, inbound rules for **SSH
  (22)** and **HTTP (80)** only, apply it to the droplet.
- Or, on the droplet itself:
  ```bash
  ufw allow 22
  ufw allow 80
  ufw enable
  ```

## 3. Install Docker

SSH in (`ssh root@<DROPLET_IP>`), then:
```bash
curl -fsSL https://get.docker.com | sh
```
This installs Docker Engine plus the `docker compose` plugin (the `docker compose` command used
throughout this repo, not the older standalone `docker-compose`).

A small swap file is still cheap insurance on a 1GB droplet running four containers at once
(Postgres, api, webapp, nginx), even though nothing gets built there anymore:
```bash
fallocate -l 1G /swapfile
chmod 600 /swapfile
mkswap /swapfile
swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab
```

## 4. Set up the GitLab + DigitalOcean side (one-time)

**a) DigitalOcean API token** - DO dashboard → API → Generate New Token, scopes: read and
write. This is used both by CI (to push) and by the droplet (to pull) - DOCR auth is your DO
account email as username, this token as password.

**b) A dedicated deploy SSH keypair** - so CI can log into the droplet without reusing your own
personal key. On your own machine:
```bash
ssh-keygen -t ed25519 -f gymcrm-deploy-key -N ""
```
Add the **public** half to the droplet: `ssh-copy-id -i gymcrm-deploy-key.pub root@<DROPLET_IP>`
(or paste its contents into `~/.ssh/authorized_keys` on the droplet by hand). Keep the private
half (`gymcrm-deploy-key`, no `.pub`) for the next step - don't commit either file anywhere.

**c) GitLab CI/CD variables** - Settings → CI/CD → Variables, add five:
- `DEPLOY_IP` = `<DROPLET_IP>` (your real IP - baked into the frontend build's API URLs, and
  used by the `deploy` job to SSH in).
- `DO_REGISTRY_NAME` = your DOCR registry's name (shown in the DO dashboard's Container
  Registry section).
- `DO_REGISTRY_EMAIL` = your DigitalOcean account email.
- `DO_REGISTRY_TOKEN` = the token from step 4a. Mark it **masked**.
- `DEPLOY_SSH_PRIVATE_KEY` = the full contents of `gymcrm-deploy-key` (the private half) from
  step 4b. Set **Type** to **File**, not the default "Variable" - GitLab can't mask a
  multi-line value like an SSH key, so leave "Masked" unchecked here (File-type variables
  aren't exposed as a raw log-visible string the same way, so this is safe). Mark it
  **protected**.

## What actually runs when

`docker-build` and `deploy` (`.gitlab-ci.yml`) only run automatically when a merge request is
merged into `master` - that's the actual deploy trigger. On a merge request's own pipeline
they're visible in the pipeline view with a manual "play" button (in case you want to test a
build/deploy from a branch without merging first), but they never run by themselves there.
`build`/`test` are unaffected and still run on everything, same as before.

Once those five variables exist, merging anything into `master` builds both images, pushes them
to DOCR, then SSHes into the droplet and runs the same pull+up you'd otherwise run by hand (steps
8-9 below become what CI does automatically from here on - you shouldn't need to run them
yourself again after the first manual deploy).

## 5. Get the compose files onto the droplet

The droplet only actually needs `docker-compose.prod.yml`, `.env.production`, `init-dbs.sql`,
and the `nginx/` folder - not the application source. Cloning the whole repo is simplest though:
```bash
git clone git@gitlab.com:mhulina/gymcrm.git
cd gymcrm
```
(Add the droplet's SSH key to your GitLab account first if cloning over SSH, or use the HTTPS
clone URL instead.)

## 6. Log the droplet into DOCR, and fill in the registry name

```bash
docker login registry.digitalocean.com -u <your-do-account-email> -p <the-do-api-token-from-4a>
```
One-time - credentials are cached in `~/.docker/config.json` on the droplet.

`docker-compose.prod.yml` has `<DOCR_REGISTRY_NAME>` placeholders for the `api`/`webapp` images -
fill those in too:
```bash
sed -i "s/<DOCR_REGISTRY_NAME>/your-actual-registry-name/g" docker-compose.prod.yml
```

## 7. Configure `.env.production`

This repo's `.env.production` already has real generated secrets (Postgres password, JWT
secret) and just needs the droplet's IP filled in for CORS. Easiest: copy your local one up:
```bash
# from your own machine, not the droplet
scp .env.production root@<DROPLET_IP>:~/gymcrm/.env.production
```
Then on the droplet, replace the `<DROPLET_IP>` placeholder with the real IP:
```bash
sed -i "s/<DROPLET_IP>/YOUR.REAL.IP.HERE/g" .env.production
```

## 8. Deploy

```bash
docker compose -f docker-compose.prod.yml --env-file .env.production pull
docker compose -f docker-compose.prod.yml --env-file .env.production up -d
```
`pull` downloads the pre-built `api`/`webapp` images (plus `postgres`/`nginx`, both official
images); `up -d` starts everything. No `--build` - the droplet never compiles anything.

## 9. Verify

```bash
curl http://<DROPLET_IP>/health   # -> Healthy
curl -o /dev/null -s -w "%{http_code}\n" http://<DROPLET_IP>/   # -> 200
```
Then open `http://<DROPLET_IP>` in a browser - the app itself.

## Redeploying later

After a new commit runs through GitLab CI (rebuilding and pushing fresh `:latest` images):
```bash
cd gymcrm
docker compose -f docker-compose.prod.yml --env-file .env.production pull
docker compose -f docker-compose.prod.yml --env-file .env.production up -d
```
Same two commands every time - no rebuilding on the droplet, ever.
