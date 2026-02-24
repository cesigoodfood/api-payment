pipeline {
  agent any

  options {
    timestamps()
    disableConcurrentBuilds()
    skipDefaultCheckout(true)
  }

  parameters {
    string(name: 'REGISTRY_URL', defaultValue: 'rg.fr-par.scw.cloud', description: 'Docker registry host (ex: rg.fr-par.scw.cloud / ghcr.io)')
    string(name: 'REGISTRY_NAMESPACE', defaultValue: 'goodfood-alan', description: 'Registry namespace / organisation / project')
    string(name: 'IMAGE_NAME', defaultValue: 'api-payment', description: 'Docker image name')
    string(name: 'REGISTRY_PASSWORD_CREDENTIALS_ID', defaultValue: 'scw-secret-key', description: 'Jenkins Secret text credentials ID (Scaleway SCW_SECRET_KEY)')
    string(name: 'REGISTRY_USERNAME', defaultValue: 'nologin', description: 'Registry username (Scaleway Container Registry uses nologin)')
    booleanParam(name: 'TRIGGER_INFRA_DEPLOY', defaultValue: true, description: 'Trigger infra deployment job after push (main/tag only)')
    string(name: 'INFRA_JOB_NAME', defaultValue: 'infra-deploy-api-payment', description: 'Jenkins job name for infra deployment pipeline')
    string(name: 'INFRA_DEPLOYMENT_FILE', defaultValue: 'gitops/services/payment-service/deployment.yaml', description: 'Deployment manifest path in infra repo')
    string(name: 'INFRA_K8S_NAMESPACE', defaultValue: 'goodfood-prod', description: 'Kubernetes namespace for api-payment')
    string(name: 'INFRA_K8S_DEPLOYMENT_NAME', defaultValue: 'api-payment', description: 'Kubernetes Deployment name')
    string(name: 'INFRA_SMOKE_URL', defaultValue: 'https://api.goodfood.alan-courtois.fr/api/payments/health', description: 'Optional smoke URL used by infra job')
  }

  environment {
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    PATH = '/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin'
    DOCKER_BIN = '/usr/bin/docker'
    DOCKERFILE_PATH = 'GoodFood.Payment/Dockerfile'
    DOCKER_BUILD_CONTEXT = 'GoodFood.Payment'
  }

  stages {
    stage('Checkout') {
      steps {
        checkout scm
        sh 'git rev-parse --short=8 HEAD'
      }
    }

    stage('Prepare metadata') {
      steps {
        script {
          String rawBranch = (env.BRANCH_NAME ?: env.GIT_BRANCH ?: '').trim()
          String tagName = (env.TAG_NAME ?: '').trim()

          if (!rawBranch) {
            rawBranch = sh(
              script: '''
                set -eu
                BRANCH="$(git symbolic-ref --short -q HEAD || true)"

                if [ -z "${BRANCH}" ] || [ "${BRANCH}" = "HEAD" ]; then
                  BRANCH="$(
                    git for-each-ref --format='%(refname:short)' refs/remotes/origin --contains HEAD \
                      | grep -v '^origin/HEAD$' \
                      | sed 's#^origin/##' \
                      | awk 'BEGIN{main=""} { if ($0 == "main") { print; exit } if (NR == 1) { main=$0 } } END { if (NR > 0 && main != "") print main }' \
                      | head -n1 \
                      || true
                  )"
                fi

                printf '%s' "${BRANCH}"
              ''',
              returnStdout: true,
            ).trim()
          }

          if (!tagName) {
            tagName = sh(
              script: "git tag --points-at HEAD | head -n1",
              returnStdout: true,
            ).trim()
          }

          rawBranch = rawBranch
            .replaceFirst(/^origin\//, '')
            .replaceFirst(/^refs\\/heads\\//, '')
            .replaceFirst(/^\\*\\//, '')
          if (rawBranch == 'HEAD') {
            rawBranch = ''
          }

          env.GIT_SHA_SHORT = sh(script: 'git rev-parse --short=8 HEAD', returnStdout: true).trim()
          env.IMAGE_TAG = env.GIT_SHA_SHORT
          env.IMAGE_REPO = "${params.REGISTRY_URL}/${params.REGISTRY_NAMESPACE}/${params.IMAGE_NAME}"
          env.EFFECTIVE_BRANCH = rawBranch
          env.IS_MAIN_BRANCH = (rawBranch == 'main').toString()
          env.IS_TAG_BUILD = (!tagName.isEmpty()).toString()
          env.SHOULD_PUSH = ((rawBranch == 'main') || !tagName.isEmpty()).toString()
          env.PUSH_LATEST = (rawBranch == 'main').toString()
          env.RELEASE_TAG = tagName ? tagName.replaceAll(/[^0-9A-Za-z_.-]/, '-') : ''
          env.SHOULD_TRIGGER_INFRA = (
            env.SHOULD_PUSH == 'true' &&
            params.TRIGGER_INFRA_DEPLOY &&
            params.INFRA_JOB_NAME?.trim()
          ).toString()

          currentBuild.displayName = "#${env.BUILD_NUMBER} ${params.IMAGE_NAME}:${env.IMAGE_TAG}"

          echo "Env branch hints: BRANCH_NAME=${env.BRANCH_NAME ?: 'n/a'} GIT_BRANCH=${env.GIT_BRANCH ?: 'n/a'} TAG_NAME=${env.TAG_NAME ?: 'n/a'}"
          echo "Branch=${rawBranch ?: 'n/a'} Tag=${tagName ?: 'n/a'}"
          echo "Image=${env.IMAGE_REPO}:${env.IMAGE_TAG}"
          echo "Push enabled=${env.SHOULD_PUSH}"
        }
      }
    }

    stage('Setup .NET') {
      steps {
        sh '''
          set -eu
          dotnet --info
          dotnet restore GoodFood.Payment.sln
        '''
      }
    }

    stage('Build') {
      steps {
        sh '''
          set -eu
          dotnet build GoodFood.Payment.sln -c Release --no-restore
        '''
      }
    }

    stage('Test') {
      steps {
        sh '''
          set -eu
          TEST_PROJECT_COUNT="$(find . -type f \\( -name '*Tests.csproj' -o -name '*.Tests.csproj' \\) | wc -l | tr -d ' ')"
          if [ "${TEST_PROJECT_COUNT}" = "0" ]; then
            echo "No test project found in api-payment. Skipping dotnet test."
            mkdir -p TestResults
            printf 'SKIPPED: no test project found\\n' > TestResults/no-tests.txt
            exit 0
          fi

          dotnet test GoodFood.Payment.sln -c Release --no-build --logger "trx;LogFileName=test-results.trx"
        '''
      }
      post {
        always {
          archiveArtifacts artifacts: '**/TestResults/*', allowEmptyArchive: true
        }
      }
    }

    stage('Preflight debug') {
      steps {
        sh '''
          set -x
          pwd
          hostname
          id
          echo "PATH=$PATH"
          command -v docker || true
          ls -l "${DOCKER_BIN}" || true
          "${DOCKER_BIN}" --version || true
          command -v dotnet || true
          dotnet --info | head -20 || true

          test -x "${DOCKER_BIN}" || {
            echo "Docker CLI not found at ${DOCKER_BIN}. Rebuild/recreate Jenkins container." >&2
            exit 1
          }
        '''
      }
    }

    stage('Docker build') {
      steps {
        sh '''
          set -eu
          "${DOCKER_BIN}" build \
            -f "${DOCKERFILE_PATH}" \
            -t "${IMAGE_REPO}:${IMAGE_TAG}" \
            "${DOCKER_BUILD_CONTEXT}"
        '''
      }
    }

    stage('Registry login') {
      when {
        expression { env.SHOULD_PUSH == 'true' }
      }
      steps {
        withCredentials([
          string(
            credentialsId: params.REGISTRY_PASSWORD_CREDENTIALS_ID,
            variable: 'REGISTRY_PASSWORD',
          ),
        ]) {
          sh '''
            set +x
            echo "${REGISTRY_PASSWORD}" | "${DOCKER_BIN}" login "${REGISTRY_URL}" -u "${REGISTRY_USERNAME}" --password-stdin
          '''
        }
      }
    }

    stage('Push image') {
      when {
        expression { env.SHOULD_PUSH == 'true' }
      }
      steps {
        script {
          sh "\"${env.DOCKER_BIN}\" push ${env.IMAGE_REPO}:${env.IMAGE_TAG}"

          if (env.PUSH_LATEST == 'true') {
            sh "\"${env.DOCKER_BIN}\" tag ${env.IMAGE_REPO}:${env.IMAGE_TAG} ${env.IMAGE_REPO}:latest"
            sh "\"${env.DOCKER_BIN}\" push ${env.IMAGE_REPO}:latest"
          }

          if (env.IS_TAG_BUILD == 'true' && env.RELEASE_TAG?.trim()) {
            sh "\"${env.DOCKER_BIN}\" tag ${env.IMAGE_REPO}:${env.IMAGE_TAG} ${env.IMAGE_REPO}:${env.RELEASE_TAG}"
            sh "\"${env.DOCKER_BIN}\" push ${env.IMAGE_REPO}:${env.RELEASE_TAG}"
          }
        }
      }
    }


  }

  post {
    always {
      script {
        sh "\"${env.DOCKER_BIN}\" logout ${params.REGISTRY_URL} >/dev/null 2>&1 || true"
      }
    }
    success {
      echo "api-payment pipeline completed successfully."
    }
  }
}
