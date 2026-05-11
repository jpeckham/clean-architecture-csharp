#!/bin/sh
set -eu

api_base_address="${API_BASE_ADDRESS:-http://localhost:8080}"

cat > /usr/share/nginx/html/appsettings.json <<EOF
{
  "ApiBaseAddress": "$api_base_address"
}
EOF

