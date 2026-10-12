#!/bin/bash
source "$(dirname "$0")/00-common.sh"
TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT

cat > "$TMP/trust.json" <<'JSON'
{
  "Version": "2012-10-17",
  "Statement": [{ "Effect": "Allow", "Principal": { "Service": "ec2.amazonaws.com" }, "Action": "sts:AssumeRole" }]
}
JSON

if aws iam get-role --role-name "$ROLE_NAME" >/dev/null 2>&1; then
  log "Role $ROLE_NAME já existe"
else
  log "Criando role $ROLE_NAME"
  aws iam create-role --role-name "$ROLE_NAME" --assume-role-policy-document "file://$TMP/trust.json" >/dev/null
fi

# Atenção: policies *FullAccess* por simplicidade acadêmica; em produção use privilégio mínimo.
for policy in AmazonS3FullAccess AmazonSNSFullAccess AmazonSQSFullAccess AmazonDynamoDBFullAccess; do
  log "Anexando $policy"
  aws iam attach-role-policy --role-name "$ROLE_NAME" --policy-arn "arn:aws:iam::aws:policy/$policy"   # idempotente
done

if aws iam get-instance-profile --instance-profile-name "$PROFILE_NAME" >/dev/null 2>&1; then
  log "Instance profile já existe"
else
  log "Criando instance profile $PROFILE_NAME"
  aws iam create-instance-profile --instance-profile-name "$PROFILE_NAME" >/dev/null
fi
aws iam add-role-to-instance-profile --instance-profile-name "$PROFILE_NAME" --role-name "$ROLE_NAME" 2>/dev/null || true

save_state IAM_INSTANCE_PROFILE "$PROFILE_NAME"
log "OK: $PROFILE_NAME (aguarde ~10s para propagação antes do passo 07)"
