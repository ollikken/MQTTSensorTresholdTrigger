# MQTT Sensor Treshold Trigger

Subsribes to an MQTT sensor and posts to a configurable URL when sensor exceeds a configurable treshold.

Takes two argumens, the treshold and the url to post to.

# Docker container

There is a dockerfile describing the build process for a docker image.

There is also a powershell script "BuildAndRunDockerContainer.ps1" to build and then run the image as a container in interactive mode. Interactive mode is used because a logger is not yet added.

Helpfull information
- http://odewahn.github.io/docker-jumpstart/building-images-with-dockerfiles.html
- https://docs.docker.com/engine/examples/dotnetcore/#create-a-dockerfile-for-an-aspnet-core-application
- Dockerfile from clean visual studio aspnet core project with docker support

# MQTT

I used the MQTT Net project to subscribe to the sensor value.
https://github.com/chkr1011/MQTTnet/wiki/Client

This was after I discovered that eclipse m2mqtt does not have .NET Core support yet.
https://github.com/eclipse/paho.mqtt.m2mqtt/issues/10

# Other Helpfull Links
- https://stackoverflow.com/questions/6117101/posting-jsonobject-with-httpclient-from-web-api
- https://www.newtonsoft.com/json/help/html/SerializeDateTimeZoneHandling.htm

