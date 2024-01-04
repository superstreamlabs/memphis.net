def gitBranch = env.BRANCH_NAME
def gitURL = "git@github.com:Memphisdev/memphis.net.git"
def repoUrlPrefix = "memphisos"

node ("memphis-jenkins-small-fleet-agent") {
  git credentialsId: 'main-github', url: gitURL, branch: gitBranch
  if (env.BRANCH_NAME ==~ /(change-jenkins-agent)/) { 
    versionTag = readFile "./version-beta.conf"
  }
  else {
    versionTag = readFile "./version.conf"
  }

  try{
    
    stage('Install .NET SDK') {
      sh """
	wget https://dot.net/v1/dotnet-install.sh
        chmod +x dotnet-install.sh
        ./dotnet-install.sh -c STS
      """
    }
    
    stage('Build project'){
      sh """
        ~/.dotnet/dotnet build -c Release src/Memphis.Client.sln
      """
    }
  
    stage('Package the project'){
      sh """
        ~/.dotnet/dotnet pack -v normal -c Release --no-restore --include-source /p:ContinuousIntegrationBuild=true -p:PackageVersion=$versionTag src/Memphis.Client/Memphis.Client.csproj
      """
    }

    stage('Publish to NuGet'){
      withCredentials([string(credentialsId: 'NUGET_KEY', variable: 'NUGET_KEY')]) {
        sh """
          ~/.dotnet/dotnet nuget push ./src/Memphis.Client/bin/Release/Memphis.Client.${versionTag}.nupkg --source https://api.nuget.org/v3/index.json --api-key $NUGET_KEY
        """
      }
    }

    if (env.BRANCH_NAME ==~ /(latest)/) {
      stage('Create new Release'){
	sh """
	  sudo dnf config-manager --add-repo https://cli.github.com/packages/rpm/gh-cli.repo -y
          sudo dnf install gh -y
          sudo dnf install jq -y
	"""
        withCredentials([sshUserPrivateKey(keyFileVariable:'check',credentialsId: 'main-github')]) {
          sh """
            git reset --hard origin/latest
            GIT_SSH_COMMAND='ssh -i $check' git checkout -b \$(cat version.conf)
            GIT_SSH_COMMAND='ssh -i $check' git push --set-upstream origin \$(cat version.conf)
  	  """
        }
        withCredentials([string(credentialsId: 'gh_token', variable: 'GH_TOKEN')]) {
          sh(script:"""gh release create \$(cat version.conf) --generate-notes""", returnStdout: true)
        }
      }
  }


    notifySuccessful()

  } catch (e) {
      currentBuild.result = "FAILED"
      cleanWs()
      notifyFailed()
      throw e
  }
}

def notifySuccessful() {
  emailext (
      subject: "SUCCESSFUL: Job '${env.JOB_NAME} [${env.BUILD_NUMBER}]'",
      body: """<p>SUCCESSFUL: Job '${env.JOB_NAME} [${env.BUILD_NUMBER}]':</p>
        <p>Check console output at &QUOT;<a href='${env.BUILD_URL}'>${env.JOB_NAME} [${env.BUILD_NUMBER}]</a>&QUOT;</p>""",
      recipientProviders: [[$class: 'DevelopersRecipientProvider']]
    )
}

def notifyFailed() {
  emailext (
      subject: "FAILED: Job '${env.JOB_NAME} [${env.BUILD_NUMBER}]'",
      body: """<p>FAILED: Job '${env.JOB_NAME} [${env.BUILD_NUMBER}]':</p>
        <p>Check console output at &QUOT;<a href='${env.BUILD_URL}'>${env.JOB_NAME} [${env.BUILD_NUMBER}]</a>&QUOT;</p>""",
      recipientProviders: [[$class: 'DevelopersRecipientProvider']]
    )
}
