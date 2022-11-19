# ActiveMQ Artemis
FROM --platform=linux/amd64 azul/zulu-openjdk-alpine:14

RUN mkdir /artemis
WORKDIR /artemis

RUN wget -O "artemis.tar" "https://www.apache.org/dyn/closer.cgi?filename=activemq/activemq-artemis/2.27.0/apache-artemis-2.27.0-bin.tar.gz&action=download"
RUN tar -xvf ./artemis.tar; \
    ln -s /artemis/apache-artemis-2.27.0/ ./current
#WORKDIR /artemis/current

RUN /artemis/current/bin/artemis create --user artemis --password artemis --http-host 0.0.0.0 --require-login --nio --relax-jolokia amq

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
