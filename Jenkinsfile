// ENV Variable needed by this Jenkinsfile
// $ECR_Repo $EKS_Cluster $DockerImage
// Jenkins default ENV Variable used:
// $BUILD_ID

pipeline {
    agent any
    
    stages {
        stage('Check_or_Install_prerequisites_on_x86_agent') {
            agent {
                label 'aws_ec2_linux_al2_x86_64'
            }
            steps {
                echo 'Check for docker and buildx plugin and install if not already installed.'
                sh '''
                # env
                if ! which docker >/dev/null 2>&1; then sudo yum install docker -y; fi
                if ! [[ $(systemctl show --property SubState docker) =~ \'running\' ]]; then sudo systemctl enable docker --now; fi
                if ! id | grep -i docker >/dev/null 2>&1; then sudo usermod -aG docker $USER; fi
                if ! docker buildx ls >/dev/null 2>&1; then
                  sudo docker run -d -it docker/buildx-bin bash >/dev/null 2>&1 || true
                  CONTAINER=$(sudo docker ps -a --filter=ancestor=docker/buildx-bin | awk \'{print $1}\' | tail -1)
                  mkdir -p /usr/libexec/docker/cli-plugins/
                  sudo docker cp $CONTAINER:/buildx /usr/libexec/docker/cli-plugins/docker-buildx
                  # sudo docker run --rm --privileged multiarch/qemu-user-static --reset -p yes
                  sudo docker buildx ls
                fi'''
            }
        }
        stage('Check_or_Install_prerequisites_on_arm_agent') {
            agent {
                label 'aws_ec2_linux_al2_arm64'
            }
            steps {
                echo 'Check for docker and buildx plugin and install if not already installed.'
                sh '''
                # env
                if ! which docker >/dev/null 2>&1; then sudo yum install docker -y; fi
                if ! [[ $(systemctl show --property SubState docker) =~ \'running\' ]]; then sudo systemctl enable docker --now; fi
                if ! id | grep -i docker >/dev/null 2>&1; then sudo usermod -aG docker $USER; fi
                if ! docker buildx ls >/dev/null 2>&1; then
                  sudo docker run -d -it docker/buildx-bin bash >/dev/null 2>&1 || true
                  CONTAINER=$(sudo docker ps -a --filter=ancestor=docker/buildx-bin | awk \'{print $1}\' | tail -1)
                  mkdir -p /usr/libexec/docker/cli-plugins/
                  sudo docker cp $CONTAINER:/buildx /usr/libexec/docker/cli-plugins/docker-buildx
                  # sudo docker run --rm --privileged multiarch/qemu-user-static --reset -p yes
                  sudo docker buildx ls
                fi'''
            }
        }
        stage ('get_x86_agent_ip') {
            agent {
                label 'aws_ec2_linux_al2_x86_64'
            }
            steps {
                script{
                    def priv_ip = sh(returnStdout: true, script: 'echo $(curl -s http://169.254.169.254/latest/meta-data/local-ipv4)').trim()
                    env.X86_AGENT_PRIVATE_IP = priv_ip
                }
            }
        }
        stage('Build') {
            agent {
                label 'aws_ec2_linux_al2_arm64'
            }
            steps {
                echo 'Building Docker Image'
                script {
                    withCredentials([
                        sshUserPrivateKey(
                            credentialsId: 'EC2-Instance-Key',
                            keyFileVariable: 'keyFile'
                        )
                    ]) {
                        sh '''
                        echo "$keyFile" > /tmp/mycredFile; cat /tmp/mycredFile;
                        cat $keyFile > ~/.ssh/id_rsa
                        chmod 600 ~/.ssh/id_rsa
                        '''
                    }
                    def amd_ip = env.X86_AGENT_PRIVATE_IP
                    echo "AMD Private IP: ${amd_ip}"
                    sh '''
                    REGION=$(curl -s 169.254.169.254/latest/meta-data/placement/region)
                    BUILDER_NAME='jenkins'
                    DOCKER='docker --config ./docker-buildx-config'
                    $DOCKER -H ssh://ec2-user@$X86_AGENT_PRIVATE_IP info
                    if ! $DOCKER buildx inspect $BUILDER_NAME > /dev/null 2>&1; then
                        $DOCKER buildx create --name $BUILDER_NAME
                        $DOCKER buildx create --name $BUILDER_NAME --driver docker-container --platform linux/amd64 ssh://ec2-user@$X86_AGENT_PRIVATE_IP --append
                        $DOCKER buildx inspect --bootstrap --builder $BUILDER_NAME
                        $DOCKER buildx use $BUILDER_NAME
                    fi
                    $DOCKER buildx ls
                    aws ecr get-login-password --region $REGION | $DOCKER login --username AWS --password-stdin $ECR_Repo
                    $DOCKER buildx build -t $ECR_Repo:$BUILD_ID --platform linux/amd64,linux/arm64 --builder jenkins \
                    --build-arg BUILDKIT_MULTI_PLATFORM=1 -f ./GadgetsOnline/Dockerfile --push .
                    '''
                }
            }
        }
        stage('Test') {
            agent {
                label 'aws_ec2_linux_al2_arm64'
            }
            steps {
                echo 'Test 1: Checking if image is multi-arch'
                sh '''
                DOCKER='docker --config ./docker-buildx-config'
                export AWS_REGION=$(curl -s 169.254.169.254/latest/meta-data/placement/region)
                aws ecr get-login-password --region $AWS_REGION | $DOCKER login --username AWS --password-stdin $ECR_Repo
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
        stage('Deploy to EKS') {
            steps {
                withAWS(credentials: 'IAM-Admin-Credential') {
                  sh '''
                  export AWS_REGION=$(curl -s 169.254.169.254/latest/meta-data/placement/region)
                  aws eks update-kubeconfig --name $EKS_Cluster
                  kubectl get pod --all-namespaces
                  sed -ie "/image:/s_nginx:latest_${DockerImage}_" ./EKSDeployment.yaml
                  yq 'select(documentIndex == 0)' EKSDeployment.yaml | kubectl apply -f -
                  yq "select(documentIndex == 1)|.spec.template.spec.containers[0].env += \
                  {\\\"name\\\": \\\"IMAGE_TAG\\\", \\\"value\\\": \\\"$BUILD_ID\\\"}" EKSDeployment.yaml | kubectl apply -f - 
                  # kubectl apply -f ./EKSDeployment.yaml
                  kubectl get svc,pod -o wide
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
