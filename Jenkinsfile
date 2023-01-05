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
          archiveArtifacts artifacts: "build/NeuroPace-WebGL.zip"
          stash includes: "build/NeuroPace-WebGL.zip", name: 'webgl-build-debug'
        }
      }
    }
    stage('Deploy to https://cat-astrophe-games.party/neuropace/app/ (Debug)') {
      agent {
        label 'jenkins-webgl-deployer'
      }
      steps {
        unstash 'webgl-build-debug'
        /* Note that the zip filename is also determined by branch, build number etc.
           to avoid having problems due to multiple builds using this script overriding
           the same zip file, and causing trouble in case of multiple executions of this script
           going in parallel.
        */
        sh '''
          CAG_PROJECT_NAME='neuropace-debug' &&
          SAFE_BRANCH_NAME=`echo -n "${BRANCH_NAME}" | tr "./_" -` &&
          ZIP_NAME=latest-$CAG_PROJECT_NAME-$SAFE_BRANCH_NAME-$BUILD_NUMBER &&
          scp build/NeuroPace-WebGL-Debug.zip neuropace@michalis.ii.uni.wroc.pl:/home/neuropace/$ZIP_NAME.zip &&
          ssh neuropace@michalis.ii.uni.wroc.pl "bash -s" -- $ZIP_NAME $CAG_PROJECT_NAME $SAFE_BRANCH_NAME $BUILD_NUMBER $GIT_COMMIT < ~/build-scripts/bin/cag-paima-update-latest-www
        '''
      }
    }
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
