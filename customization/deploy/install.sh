#!/usr/bin/env bash
set -Eeuo pipefail
REPO_URL="https://github.com/lbsus-sy/MeshCentral.git"
BRANCH="custom/ui-deploy-foundation"
INSTALL_DIR="/opt/lakers-meshcentral"

if [[ $EUID -ne 0 ]]; then
  echo "Run as root, for example: curl -fsSL https://raw.githubusercontent.com/lbsus-sy/MeshCentral/custom/ui-deploy-foundation/customization/deploy/install.sh | sudo bash"
  exit 1
fi
if ! grep -qiE 'ubuntu|debian' /etc/os-release; then echo "This one-click installer supports Ubuntu/Debian only."; exit 1; fi

echo "=== Lakers MeshCentral one-click setup ==="
echo "Before continuing, point your DNS A record at this VPS and allow TCP 80/443 in its firewall."
read -r -p "Enter the DNS hostname (e.g. mesh.example.com): " MESH_HOSTNAME
if [[ ! "$MESH_HOSTNAME" =~ ^[A-Za-z0-9.-]+\.[A-Za-z]{2,}$ ]]; then echo "Invalid hostname. Configure DNS first, then retry." >&2; exit 1; fi
export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y ca-certificates curl git docker.io
if ! docker compose version >/dev/null 2>&1; then
  apt-get install -y docker-compose-v2 || apt-get install -y docker-compose-plugin
fi
systemctl enable --now docker
if [[ -e "$INSTALL_DIR" ]]; then echo "Install directory already exists: $INSTALL_DIR. Stopping to protect existing data." >&2; exit 1; fi
git clone --depth 1 --branch "$BRANCH" "$REPO_URL" "$INSTALL_DIR"
cd "$INSTALL_DIR"
cp customization/deploy/.env.example customization/deploy/.env
sed -i "s/^MESH_HOSTNAME=.*/MESH_HOSTNAME=$MESH_HOSTNAME/" customization/deploy/.env
docker compose -f customization/deploy/compose.yaml --env-file customization/deploy/.env up -d --build
echo
echo "=== Install command completed ==="
echo "Open https://$MESH_HOSTNAME"
echo "Caddy will request and renew a trusted HTTPS certificate when DNS and ports 80/443 are reachable."
echo "Create the first administrator, enable MFA, and keep public account registration disabled."
echo "Logs: cd $INSTALL_DIR && docker compose -f customization/deploy/compose.yaml --env-file customization/deploy/.env logs -f"
