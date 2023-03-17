pipeline {
    agent any
    
    stages {
        stage('Install') {
            steps {
                echo 'Check for docker and buildx plugin and install if not already installed.'
                sh '''
                whoami && id
                if ! which docker >/dev/null 2>&1; then sudo yum install docker -y; fi
                if ! [[ $(systemctl show --property ActiveState docker) =~ \'active\' ]]; then sudo systemctl enable docker --now; fi
                if ! docker buildx ls >/dev/null 2>&1; then
                  sudo docker run -d -it docker/buildx-bin bash >/dev/null 2>&1
                  CONTAINER=$(sudo docker ps -a --filter=ancestor=docker/buildx-bin | awk \'{print $1}\' | tail -1)
                  mkdir -p /usr/libexec/docker/cli-plugins/
                  sudo docker cp $CONTAINER:/buildx /usr/libexec/docker/cli-plugins/docker-buildx
                  sudo docker run --rm --privileged multiarch/qemu-user-static --reset -p yes
                  sudo docker buildx create --name $HOSTNAME --use
                  sudo docker buildx inspect --bootstrap
                  sudo docker buildx ls
                fi'''
            }
        }
        stage('Build') {
            steps {
                echo 'Building Docker Image'
                sh '''
                DOCKER="docker --config ./docker-buildx-config"
                $DOCKER buildx create --name jenkins --use
                $DOCKER buildx inspect --bootstrap
                $DOCKER buildx ls
                $DOCKER buildx build -t $ECR_Repo:$BUILD_ID --platform linux/amd64,linux/arm64 --builder jenkins \
                --build-arg BUILDKIT_MULTI_PLATFORM=1 -f ./GadgetsOnline/Dockerfile --push .
                env'''
            }
        }
        stage('Test') {
            steps {
                echo 'Testing..'
                echo 'Done!'
            }
        }
    }
}
