pipeline {
    agent any
    
    stages {
        stage('Install') {
            steps {
                echo 'Check for docker and buildx plugin and install if not already installed.'
                sh '''env
                if ! which docker >/dev/null 2>&1; then sudo yum install docker -y; fi
                if ! [[ $(systemctl show --property ActiveState docker) =~ \'active\' ]]; then sudo systemctl enable docker --now; fi
                if ! docker buildx ls >/dev/null 2>&1; then
                  sudo docker run -d -it docker/buildx-bin bash >/dev/null 2>&1 || true
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
                REGION=$(curl -s 169.254.169.254/latest/meta-data/placement/region)
                DOCKER='docker --config ./docker-buildx-config'
                $DOCKER buildx create --name jenkins --use
                $DOCKER buildx inspect --bootstrap
                $DOCKER buildx ls
                aws ecr get-login-password --region $REGION | $DOCKER login --username AWS --password-stdin $ECR_Repo
                $DOCKER buildx build -t $ECR_Repo:$BUILD_ID --platform linux/amd64,linux/arm64 --builder jenkins \
                --build-arg BUILDKIT_MULTI_PLATFORM=1 -f ./GadgetsOnline/Dockerfile --push .
                '''
            }
        }
        stage('Test') {
            steps {
                echo 'Test 1: Checking if image is multi-arch'
                sh '''
                DOCKER='docker --config ./docker-buildx-config'
                PLATFORMS=$($DOCKER buildx imagetools inspect $ECR_Repo:$BUILD_ID --format "{{json .Manifest}}" | jq ".manifests[].platform.architecture" | xargs)
                if [[ $PLATFORMS =~ "arm" ]] && [[ $PLATFORMS =~ "amd" ]]; then
                  echo "Image is multi-arch. Test PASSED!!"
                else
                  echo "Image is not multi-arch. Test FAILED."
                  exit 1
                fi
                '''
                echo 'Test 2: Checking if container is being created from the image.'
                sh '''
                DOCKER='docker --config ./docker-buildx-config'
                DOCKER_ID=$($DOCKER run -d -p 8888:80 $ECR_Repo:$BUILD_ID)
                sleep 5
                HTTP_RESP=$(curl -s -o /dev/null -I -w "%{http_code}" localhost:8888)
                # $DOCKER stop $($DOCKER ps -q --filter="ancestor=$ECR_Repo:$BUILD_ID")
                $DOCKER stop $DOCKER_ID
                if [[ $HTTP_RESP == "200" ]]; then
                  echo "Container launched successfully. Test PASSED!!"
                else
                  echo "Container could not launch successfully. Test Failed."
                  exit 1
                fi
                '''
            }
        }
        stage('Deploy') {
            steps {
                withAWS(credentials: 'IAM-Admin-Credential') {
                  sh '''
                  export AWS_REGION=$(curl -s 169.254.169.254/latest/meta-data/placement/region)
                  aws sts get-caller-identity
                  aws eks update-kubeconfig --name $EKS_Cluster
                  kubectl get pod --all-namespaces
                  DockerImage=$Docker_Image
                  sed -ie "/image:/s_nginx:latest_${DockerImage}_" ./EKSDeployment.yaml
                  kubectl apply -f ./EKSDeployment.yaml
                  '''
                }
            }
        }
    }
    post {
        always {
            echo 'Deleting Workspace'
            cleanWs deleteDirs: true
        }
    }
}
