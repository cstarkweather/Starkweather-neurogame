/* -*- mode: groovy -*-
  Confgure how to run our job in Jenkins.
  See https://castle-engine.io/jenkins .
*/

library 'cag-shared-jenkins-library'

pipeline {
  options {
    /* We do not really have a problem with concurrent builds (they work),
       but they slow down Jenkins too much with too many long-running builds.
       Better to wait for previous build to finish. */
    disableConcurrentBuilds()
    /* workaround Unity crashes */
    retry(1)
  }
  agent none // each stage has a particular agent

  stages {
    stage('Clean') {
      agent {
        label 'windows-unity-builder'
      }
      steps {
        sh "cag-clean-repo ."
        dir ("NeuroPaceUnityProject") {
          sh 'rm -Rf build/'
        }
      }
    }
    stage('Build Windows64 (Debug)') {
      agent {
        label 'windows-unity-builder'
      }
      steps {
        dir ("NeuroPaceUnityProject") {
          sh 'cag-build-windows64 Debug'
          archiveArtifacts artifacts: "build/*.zip"
        }
      }
    }
    stage('Build Linux64 (Debug)') {
      agent {
        label 'windows-unity-builder'
      }
      steps {
        dir ("NeuroPaceUnityProject") {
          sh 'cag-build-linux64 Debug'
          archiveArtifacts artifacts: "build/*.zip"
        }
      }
    }
    stage('Build WebGL (Debug)') {
      agent {
        label 'windows-unity-builder'
      }
      steps {
        dir ("NeuroPaceUnityProject") {
          sh 'cag-build-webgl Debug'
          archiveArtifacts artifacts: "build/*.zip"
        }
      }
    }
    /* TODO: deploy */
  }
  post {
    success {
      discordSend title: "NeuroPace (${env.BRANCH_NAME})",
        description: getDiscordNotification(currentBuild) + "- Build Successful",
        link: env.BUILD_URL,
        result: currentBuild.currentResult,
        webhookURL: 'https://discordapp.com/api/webhooks/521028168874852358/O__azOG-z4v-tZ3PeEOqUbtOGPqulEBZPSgxWu78EOsz-id8ssmRm82xTnrHAW8vtZ7V',
        successful: true
    }
    failure {
      discordSend title: "NeuroPace (${env.BRANCH_NAME})",
        description: getDiscordNotification(currentBuild) + "- Build Failed",
        link: env.BUILD_URL,
        result: currentBuild.currentResult,
        webhookURL: 'https://discordapp.com/api/webhooks/521028168874852358/O__azOG-z4v-tZ3PeEOqUbtOGPqulEBZPSgxWu78EOsz-id8ssmRm82xTnrHAW8vtZ7V',
        successful: false
    }
  }
}
