#!/bin/bash
source "$(dirname "$0")/00-common.sh"

log "S3: bucket $BUCKET"
if aws s3api head-bucket --bucket "$BUCKET" 2>/dev/null; then
  log "Bucket já existe"
else
  if [ "$REGION" = "us-east-1" ]; then
    aws s3api create-bucket --bucket "$BUCKET"
  else
    aws s3api create-bucket --bucket "$BUCKET" --create-bucket-configuration LocationConstraint="$REGION"
  fi
fi

log "Bloqueando acesso público"
aws s3api put-public-access-block --bucket "$BUCKET" --public-access-block-configuration \
  BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true

log "Habilitando versionamento"
aws s3api put-bucket-versioning --bucket "$BUCKET" --versioning-configuration Status=Enabled

save_state S3_BUCKET "$BUCKET"
log "OK: $BUCKET"
