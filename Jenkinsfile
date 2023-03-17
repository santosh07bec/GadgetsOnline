pipeline {
    agent any
    
    stages {
        stage('Install') {
            steps {
                sh '''if ! which docker >/dev/null 2>&1; then sudo yum install docker -y; fi
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
                sh '''echo $(pwd)
                ls -l
                echo \'to know where it is registered\'
                tree .'''
            }
        }
        stage('Test') {
            steps {
                echo 'Testing..'
            }
        }
    }
}
