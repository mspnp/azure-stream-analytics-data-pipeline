#!/bin/bash



# Set variables for the new account, database, and collection
resourceGroup='asa-ra-2-test'
resourceLocation='southcentralus'
cosmosDatabaseAccount='docdb-taxi-asa-ra-2-test'
cosmosDatabase='docdb-taxicab-database'
cosmosDataBaseCollection='docdb-taxicabavgtrippermile-collection'
eventHubNamespace='asa-ra-2-test-testeventhub'

echo "resource group = $resourceGroup"
echo "resourceLocation = $resourceLocation"
echo "cosmosDatabaseAccount = $cosmosDatabaseAccount"
echo "cosmosDatabase = $cosmosDatabase"
echo "cosmosDataBaseCollection = $cosmosDataBaseCollection"




echo "creating resource group $resourceGroup"
# Create a resource group
az group create --name $resourceGroup --location $resourceLocation


echo "creating cosmos account $cosmosDatabaseAccount"
# Create a DocumentDB API Cosmos DB account
az cosmosdb create --name $cosmosDatabaseAccount --kind GlobalDocumentDB --resource-group $resourceGroup --max-interval 10 --max-staleness-prefix 200 


echo "creating cosmos db $cosmosDatabase"
# Create a database 
az cosmosdb database create --name $cosmosDatabaseAccount --db-name $cosmosDatabase --resource-group $resourceGroup


echo "creating cosmos collection $cosmosDataBaseCollection"
# Create a collection
az cosmosdb collection create --collection-name $cosmosDataBaseCollection --name $cosmosDatabaseAccount --db-name $cosmosDatabase --resource-group $resourceGroup


echo "creating asa job"
# Create 2 event hub , one storage account and a asa job
az group deployment create --resource-group $resourceGroup --template-file ./deployresources.json --parameters eventHubNamespace=$eventHubNamespace outputCosmosDatabaseAccount=$cosmosDatabaseAccount outputCosmosDatabase=$cosmosDatabase outputCosmosDatabaseCollection=$cosmosDataBaseCollection