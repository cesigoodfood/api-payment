# Jenkins CI local (POC) pour `api-payment`

Ce document décrit une configuration Jenkins locale démontrable pour le POC GoodFood.

## 1. Objectif

Pipeline Jenkins pour `api-payment` (.NET 8) :

1. checkout
2. restore
3. build
4. test
5. docker build
6. docker push (main/tag uniquement)
7. déclenchement d'un job Jenkins infra (GitOps/Scaleway)

## 2. Lancer Jenkins en local (Docker Compose)

Depuis `api-payment/` :

```bash
cd ci
docker compose up -d --build
```

Jenkins sera accessible sur :

- `http://localhost:8088`

### Mot de passe initial

```bash
docker exec goodfood-jenkins cat /var/jenkins_home/secrets/initialAdminPassword
```

## 3. Choix technique (démo)

Cette stack monte le socket Docker hôte dans Jenkins :

- volume : `/var/run/docker.sock:/var/run/docker.sock`

Avantage :

- très simple pour une démo locale (build/push Docker sans agent additionnel)

Risque (important) :

- un job Jenkins peut piloter Docker sur la machine hôte (privilèges élevés)
- à éviter en production sans isolation dédiée (agents éphémères, VM dédiée, Kubernetes agents, etc.)

## 4. Plugins Jenkins requis

Les plugins sont préinstallés via `ci/plugins.txt`.

Plugins inclus :

- `workflow-aggregator` (Pipeline)
- `pipeline-stage-view`
- `git`
- `github-branch-source` (multibranch GitHub)
- `credentials`
- `credentials-binding`
- `docker-workflow`
- `blueocean` (optionnel, UI)

## 5. Outils intégrés dans l’image Jenkins locale

Le contrôleur Jenkins custom contient :

- `dotnet-sdk-8.0`
- `docker` CLI
- `kubectl`
- `git`

Cela permet d'exécuter le pipeline `api-payment` et le job `infra` dans la même démo locale.

## 6. Credentials Jenkins à créer (aucun secret en dur)

### Pour le job `api-payment`

1. `registry-credentials` (type `Username with password`)
- username : utilisateur registry (ex: Scaleway registry namespace robot/user)
- password : token / mot de passe registry

### Pour le job `infra` (déploiement GitOps)

1. `infra-git-push` (type `Username with password`)
- username : `git` (ou votre user GitHub)
- password : PAT GitHub avec permission push sur le repo infra

2. `scw-goodfood-kubeconfig` (type `Secret file`)
- fichier kubeconfig du cluster Scaleway

## 7. Jobs Jenkins à créer

## Job A : `api-payment-ci` (repo `api-payment`)

Type recommandé :

- `Pipeline script from SCM` (Git)
- Jenkinsfile path : `Jenkinsfile`

Variables/params utiles (défaut dans le Jenkinsfile) :

- `REGISTRY_URL` (ex: `rg.fr-par.scw.cloud`)
- `REGISTRY_NAMESPACE`
- `IMAGE_NAME=api-payment`
- `REGISTRY_CREDENTIALS_ID=registry-credentials`
- `INFRA_JOB_NAME=infra-deploy-api-payment`

## Job B : `infra-deploy-api-payment` (repo `infra`)

Type recommandé :

- `Pipeline script from SCM`
- Jenkinsfile path : `Jenkinsfile`

Le job reçoit `IMAGE_TAG` depuis le pipeline `api-payment`.

## 8. Comportement attendu du Jenkinsfile `api-payment`

- Build/Test sur toutes les branches
- Push image uniquement :
  - branche `main`
  - ou build taggé (release tag)
- Tags poussés :
  - `${commitShaShort}` (toujours si push autorisé)
  - `latest` (uniquement sur `main`)
  - `${TAG_NAME}` (si build sur tag Git)

## 9. Variables d'environnement / paramètres (rappel)

Variables Jenkins utilisées :

- `BRANCH_NAME`
- `GIT_BRANCH`
- `GIT_COMMIT`
- `TAG_NAME` (si build de tag)

Paramètres de pipeline :

- `REGISTRY_URL`
- `REGISTRY_NAMESPACE`
- `IMAGE_NAME`
- `INFRA_JOB_NAME`
- `INFRA_DEPLOYMENT_FILE`
- `INFRA_K8S_NAMESPACE`
- `INFRA_K8S_DEPLOYMENT_NAME`
- `INFRA_SMOKE_URL`

## 10. Dépannage rapide

### `docker: permission denied`

Le socket Docker hôte peut nécessiter :

- lancer Jenkins en `user: root` (déjà fait dans `ci/docker-compose.yml`)

### Push registry refusé

- vérifier `registry-credentials`
- vérifier que le repo/image existe côté registry
- vérifier que `REGISTRY_URL` et `REGISTRY_NAMESPACE` sont corrects

### Le stage `Trigger infra deploy` ne démarre pas

- vérifier `TRIGGER_INFRA_DEPLOY=true`
- vérifier `INFRA_JOB_NAME`
- vérifier les permissions Jenkins pour déclencher le job cible

