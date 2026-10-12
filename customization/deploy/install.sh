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
read -r -p "Enter a DNS hostname pointing to this VPS (e.g. mesh.example.com): " MESH_HOSTNAME
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
echo "Install command completed. Open https://$MESH_HOSTNAME"
echo "Ports TCP 80 and 443 must be allowed in the VPS firewall and DNS must point to this server."
echo "IMPORTANT: this simple setup does not yet configure a publicly trusted TLS certificate. Do not use it for sensitive production access until trusted TLS is configured."
echo "Create the first administrator account, enable MFA, and keep account registration disabled."
echo "Logs: cd $INSTALL_DIR && docker compose -f customization/deploy/compose.yaml --env-file customization/deploy/.env logs -f meshcentral"
