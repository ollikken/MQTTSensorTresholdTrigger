# MQTTSensorTresholdTrigger

Subsribes to an MQTT sensor and posts to a configurable URL when sensor exceeds a configurable treshold.

Takes two argumens, the treshold and the url to post to.

# Docker container

There is a dockerfile describing the build process for a docker image.

There is also a powershell script "BuildAndRunDockerContainer.ps1" to build and then run the image as a container in interactive mode. Interactive mode is used because a logger is not yet added.