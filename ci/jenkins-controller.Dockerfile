FROM jenkins/jenkins:lts-jdk17

USER root

ARG KUBECTL_VERSION=v1.30.6
ARG DOTNET_SDK_VERSION=8.0.418

RUN apt-get update \
  && apt-get install -y --no-install-recommends ca-certificates curl gnupg git lsb-release apt-transport-https docker.io \
  && ARCH="$(dpkg --print-architecture)" \
  && case "${ARCH}" in amd64) K_ARCH=amd64; DOTNET_ARCH=x64 ;; arm64) K_ARCH=arm64; DOTNET_ARCH=arm64 ;; *) echo "Unsupported arch: ${ARCH}" >&2; exit 1 ;; esac \
  && curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
  && chmod +x /tmp/dotnet-install.sh \
  && /tmp/dotnet-install.sh --version "${DOTNET_SDK_VERSION}" --architecture "${DOTNET_ARCH}" --install-dir /usr/share/dotnet \
  && ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet \
  && rm -f /tmp/dotnet-install.sh \
  && curl -fsSL "https://dl.k8s.io/release/${KUBECTL_VERSION}/bin/linux/${K_ARCH}/kubectl" -o /usr/local/bin/kubectl \
  && chmod +x /usr/local/bin/kubectl \
  && apt-get clean \
  && rm -rf /var/lib/apt/lists/*

COPY plugins.txt /usr/share/jenkins/ref/plugins.txt
RUN jenkins-plugin-cli --plugin-file /usr/share/jenkins/ref/plugins.txt

USER jenkins
