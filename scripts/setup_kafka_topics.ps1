# Name of the Kafka container
$kafkaContainer = "0d38e922cd18748ee6d20e4c594e05700111de41577ae5fe98716660f86ee44f"

# Common settings
$partitions = 3
$replicationFactor = 1

# List of topics to create
$topics = @("game-started", "bet-placed")

foreach ($topic in $topics) {
    Write-Host "Creating topic: $topic"
    docker exec -it $kafkaContainer kafka-topics `
        --create `
        --if-not-exists `
        --bootstrap-server localhost:9092 `
        --replication-factor $replicationFactor `
        --partitions $partitions `
        --topic $topic
}