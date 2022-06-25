# ActiveMQ Artemis
FROM alpine:latest

RUN mkdir /artemis
WORKDIR /artemis

RUN apk add libaio wget
RUN wget -O jre14.tar https://cdn.azul.com/zulu/bin/zulu14.28.21-ca-jre14.0.1-linux_musl_x64.tar.gz
RUN tar -xvf ./jre14.tar
RUN ln -s /artemis/zulu14.28.21-ca-jre14.0.1-linux_musl_x64/bin/java /usr/bin/java
RUN java --version

RUN wget -O "artemis.tar" "https://www.apache.org/dyn/closer.cgi?filename=activemq/activemq-artemis/2.23.1/apache-artemis-2.23.1-bin.tar.gz&action=download"
RUN tar -xvf ./artemis.tar; \
    ln -s /artemis/apache-artemis-2.23.1/ ./current
#WORKDIR /artemis/current
ENV JAVA_HOME /artemis/zulu14.28.21-ca-jre14.0.1-linux_musl_x64/
RUN /artemis/current/bin/artemis create --user artemis --password artemis --http-host 0.0.0.0 --require-login --relax-jolokia amq

# Web Server
EXPOSE 8161 \
    # JMX Exporter
    9404 \
    # Port for CORE,MQTT,AMQP,HORNETQ,STOMP,OPENWIRE
    61616 \
    # Port for HORNETQ,STOMP
    5445 \
    # Port for AMQP
    5672 \
    # Port for MQTT
    1883 \
    #Port for STOMP
    61613

ENTRYPOINT ["/artemis/amq/bin/artemis", "run"]