# Name of the Kafka container
$kafkaContainer = "3b5fd4d432dd2e65fb1fe52c5ec409f1209c2c4ce3b163c0efd0fb48758a8c72"

# Common settings
$partitions = 3
$replicationFactor = 1

# List of topics to create
$topics = @("game-started", "bet-placed", "start-session")

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