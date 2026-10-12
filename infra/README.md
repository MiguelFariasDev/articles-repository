# Infra AWS (Partes 1 e 2)

Scripts bash com **AWS CLI v2**, idempotentes (verificam se o recurso existe antes de criar). Variáveis no topo de `00-common.sh` (`REGION`, `PROJECT`, ...); cada uma pode ser sobrescrita pelo ambiente (`REGION=us-east-2 ./01-create-s3.sh`).

## Pré-requisitos
- AWS CLI v2 configurada (`aws configure`) com permissões administrativas na conta.
- O código precisa estar num repositório Git acessível pela EC2 (`REPO_URL`).
  Para repositório privado, use uma URL com token (`https://<token>@github.com/<usuario>/articles-repository.git`) ou torne-o público durante a demo.

## Ordem de execução

```bash
cd infra
./01-create-s3.sh          # bucket articles-repository-pdfs-<ACCOUNT_ID> (privado, versionado)
./02-create-rds.sh         # RDS PostgreSQL + sg-ec2/sg-rds (5-10 min)
./03-create-elasticache.sh # Redis single-node + sg-redis (3-8 min)
./04-create-dynamodb.sh    # tabela articles-repository-logs + TTL em "Ttl"
./05-create-sns-sqs.sh     # tópico, fila, DLQ (maxReceiveCount=3), subscription
./06-create-iam-role.sh    # role + instance profile da EC2
REPO_URL=https://github.com/<usuario>/articles-repository.git ./07-create-ec2.sh
```

Opcionais no passo 07: `KEY_NAME=<par-de-chaves>` (para SSH) e `MY_IP=<ip>` (padrão: seu IP público detectado).
Ao final ele imprime o IP público; o bootstrap leva ~3-5 min. Acompanhe com `ssh ec2-user@<ip> sudo tail -f /var/log/articles-bootstrap.log`.

## Parte 2 — ALB + Auto Scaling

```bash
./08-create-alb.sh                                       # sg-alb, target group (/health), ALB HTTP:80
REPO_URL=https://github.com/<usuario>/articles-repository.git ./09-create-asg.sh   # launch template + ASG + alarmes
```

- **ALB** internet-facing em 2+ subnets da VPC padrão; target group porta 5000, health check `GET /health` (200), deregistration 30 s.
- **Segurança:** `sg-ec2` passa a aceitar a porta 5000 **somente do `sg-alb`** (o script 08 revoga o `0.0.0.0/0` da Parte 1).
- **ASG** `articles-repository-asg`: min=1, desired=1, max=3, health check `ELB` (grace 600 s, pois o bootstrap compila na instância), mesmo user-data do passo 07 (API + Worker em cada instância).
- **Escala por CPU** (alarmes CloudWatch + políticas simples, cooldown 300 s): média > 70% por 2×1 min ⇒ +1 instância; média < 25% por 2×1 min ⇒ −1. (Target tracking não permite limites diferentes para subir/descer.)
- Se a instância avulsa da Parte 1 (script 07) existir, encerre-a após o ASG ficar saudável.
- URL final: `http://<ALB_DNS>` (impresso no 08 e salvo em `.state.env`). Swagger em `/swagger`, health em `/health`.

### Demo de escala
```bash
for i in $(seq 1 8); do (while true; do curl -s "http://<ALB_DNS>/api/articles/_load?iterations=2000000" >/dev/null; done) & done
aws autoscaling describe-auto-scaling-groups --auto-scaling-group-names articles-repository-asg --query 'AutoScalingGroups[0].Instances[].[InstanceId,LifecycleState]'
kill $(jobs -p)   # CPU cai e, após alguns minutos, o ASG reduz
```
Nota: instâncias t3.micro usam créditos de CPU; a métrica de 1 min exige monitoramento detalhado (já habilitado no launch template).

## Como o `REPO_URL` e a configuração chegam à EC2

`user-data.sh` é um **template**. O `07-create-ec2.sh` substitui os placeholders `__REPO_URL__`, `__DB_CONNECTION__`, `__REDIS_CONNECTION__`, `__S3_BUCKET__`, `__SNS_TOPIC_ARN__`, `__SQS_QUEUE_URL__`, etc. usando os valores gravados pelos scripts anteriores em `infra/.state.env` (ignorado pelo Git).
Na instância, os valores ficam em `/etc/articles.env` (modo 600), lido pelos serviços systemd (`articles-api`, `articles-worker`) via `EnvironmentFile`. As variáveis `Secao__Chave` sobrescrevem o `appsettings.json`.

> ⚠️ A senha do RDS passa pelo user-data (visível a quem tem acesso à instância/EC2 API). Aceitável para o trabalho acadêmico; em produção use Secrets Manager ou SSM Parameter Store.

## Credenciais AWS: sem hardcode

A EC2 usa a **IAM role** `articles-repository-ec2-role` (instance profile). O AWSSDK resolve as credenciais automaticamente pela cadeia padrão (metadata da instância) — nenhuma chave existe no código, no `appsettings` ou no user-data. Em produção `AWS:ServiceUrl` fica vazio, então nenhum cliente aponta para o LocalStack.
As policies anexadas são `*FullAccess` (S3, SNS, SQS, DynamoDB) por simplicidade; restrinja aos recursos do projeto em um ambiente real.

## Migrations

`Database__MigrateOnStartup=true` faz a API aplicar as migrations ao subir (o lock do EF evita corrida entre instâncias do Auto Scaling).

## Limpeza

```bash
./99-cleanup.sh            # pede confirmação digitando o nome do projeto (ou FORCE=1)
```
Remove EC2, IAM, SNS/SQS, DynamoDB, ElastiCache, RDS (sem snapshot final), S3 (inclusive versões) e security groups.

## Observação

Estes scripts **não foram executados** contra uma conta AWS real durante o desenvolvimento (só validados com `bash -n`); rode-os primeiro num ambiente de teste.
