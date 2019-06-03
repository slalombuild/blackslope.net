cd ../src
echo "Docker Compose"
docker-compose up -d
cd ../scripts
echo -e "\e[32mdb container might not be ready yet, so wait a few seconds!\e[0m"
sleep 5
echo "Update database"
./db-update.sh