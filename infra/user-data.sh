#!/bin/bash
# Template de cloud-init da EC2. O 07-create-ec2.sh substitui os placeholders __XXX__ antes de enviar.
# Credenciais AWS vêm da IAM role da instância (nenhuma chave no código ou aqui).
set -euxo pipefail
exec > >(tee /var/log/articles-bootstrap.log) 2>&1

export HOME=/root DOTNET_CLI_HOME=/root DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
REPO_URL="__REPO_URL__"

# 1. .NET 10 (SDK, pois o publish acontece na própria instância)
dnf install -y git unzip
dnf install -y dotnet-sdk-10.0 || true
if ! command -v dotnet >/dev/null 2>&1; then
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet
  ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
fi
dotnet --info | head -5

# 2. Clonar o repositório
rm -rf /opt/articles && mkdir -p /opt/articles
git clone "$REPO_URL" /opt/articles

# 3. Publicar API e Worker
cd /opt/articles
dotnet publish src/ArticlesRepository.Api -c Release -o /opt/articles/api
dotnet publish src/ArticlesRepository.Worker -c Release -o /opt/articles/worker

# 4. Configuração via variáveis de ambiente (sobrescrevem o appsettings.json)
cat >/etc/articles.env <<'ENVFILE'
ConnectionStrings__Rds=__DB_CONNECTION__
Redis__Connection=__REDIS_CONNECTION__
AWS__Region=__AWS_REGION__
AWS__S3Bucket=__S3_BUCKET__
AWS__SnsTopicArn=__SNS_TOPIC_ARN__
AWS__SqsQueueUrl=__SQS_QUEUE_URL__
AWS__DynamoDbTable=__DYNAMODB_TABLE__
Database__MigrateOnStartup=true
ENVFILE
chmod 600 /etc/articles.env

# 5. Serviços systemd
cat >/etc/systemd/system/articles-api.service <<'UNIT'
[Unit]
Description=Articles Repository API
After=network.target

[Service]
WorkingDirectory=/opt/articles/api
ExecStart=/usr/bin/dotnet /opt/articles/api/ArticlesRepository.Api.dll
EnvironmentFile=/etc/articles.env
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Environment=ASPNETCORE_ENVIRONMENT=Production
Restart=always

[Install]
WantedBy=multi-user.target
UNIT

cat >/etc/systemd/system/articles-worker.service <<'UNIT'
[Unit]
Description=Articles Repository Worker
After=network.target

[Service]
WorkingDirectory=/opt/articles/worker
ExecStart=/usr/bin/dotnet /opt/articles/worker/ArticlesRepository.Worker.dll
EnvironmentFile=/etc/articles.env
Environment=DOTNET_ENVIRONMENT=Production
Restart=always

[Install]
WantedBy=multi-user.target
UNIT

systemctl daemon-reload
systemctl enable --now articles-api articles-worker

echo "Bootstrap concluído."
