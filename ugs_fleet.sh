# install ugs
# curl -sLo ugs_installer ugscli.unity.com/v1 && shasum -c <<<"3bbc507d4776a20d5feb4958be2ab7d4edcea8eb  ugs_installer" && bash ugs_installer

# https://dashboard.unity3d.com/admin-portal/organizations/18967362123070/projects/0c8a9f49-ebe7-444a-a5ee-a1d1807cf317/settings/general
# Multiplayer-Sample 0c8a9f49-ebe7-444a-a5ee-a1d1807cf317
PROJECT_ID='0c8a9f49-ebe7-444a-a5ee-a1d1807cf317'

# create service account from https://dashboard.unity3d.com/admin-portal/organizations/18967362123070/settings/service-accounts
# Don't forget give permissions to account
ACCOUNT_KEY='acb97b4c-6215-4c84-92ed-79a6696d0a35'
ACCOUNT_SECRET='d3vRw4D507IghV3AzylPSC6nwBwK6-7V'

BINARY_DIR='/Users/cozdemir/Desktop/multiplayerexport'
BINARY_PATH='x.x86_64'
ENVIRONMENT='production'
BUILD_NAME='build01'
BUILD_CONFIGURATION_NAME='buildConf'
OS='linux'
COMMAND_LINE="-port \$\$port\$\$ -queryport \$\$query_port\$\$ -logFile \$\$log_dir\$\$/Engine.log -dedicatedServer -nographics -batchmod"
FLEET_NAME='fleettest'
EUROPE_REGION_ID='859da37d-4821-11ea-bcbc-ec0d9af2b8e1'
FLEET_EUROPE_REGION_ID='98c78858-370d-4509-9d8b-4c63f502ca32'
MAX_SERVER='1'
MIN_SERVER='1'


export UGS_CLI_SERVICE_KEY_ID=${ACCOUNT_KEY}
export UGS_CLI_SERVICE_SECRET_KEY=${ACCOUNT_SECRET}
ugs config set project-id ${PROJECT_ID}
ugs config set environment-name ${ENVIRONMENT}

BUILD=$(ugs game-server-hosting build create --name ${BUILD_NAME} --os-family ${OS} -e ${ENVIRONMENT} --type FILEUPLOAD)
BUILD_ID=$(echo "${BUILD}" | grep 'buildId' | awk '{print $2}')
echo "BUILD_ID: ${BUILD_ID}"

sleep 10
echo "Deploying export files.."
ugs game-server-hosting build create-version ${BUILD_ID} -e ${ENVIRONMENT} --remove-old-files --directory ${BINARY_DIR}

sleep 200
BUILD_CONF=$(ugs game-server-hosting build-configuration create -e ${ENVIRONMENT} --name ${BUILD_CONFIGURATION_NAME} -p ${PROJECT_ID} --query-type s2p --build ${BUILD_ID} --binary-path ${BINARY_PATH} --memory 1000 --cores 1 --speed 100 --command-line "${COMMAND_LINE}")
BUILD_CONF_ID=$(echo "${BUILD_CONF}" | grep 'id' | awk '{print $2}')
echo "BUILD_CONFIG_ID: ${BUILD_CONF_ID}"

sleep 10
FLEET=$(ugs game-server-hosting fleet create --name ${FLEET_NAME} --os-family ${OS} --build-configuration-id ${BUILD_CONF_ID} --region-id ${EUROPE_REGION_ID})
FLEET_ID=$(echo "${FLEET}" | awk '/id:/ { if (!x) {print $2; x=1} }')
sleep 10
echo "FLEET_ID: ${FLEET_ID}"

sleep 20 
ugs game-server-hosting fleet-region update --fleet-id ${FLEET_ID} --region-id ${FLEET_EUROPE_REGION_ID} --min-available-servers ${MIN_SERVER} --max-servers ${MAX_SERVER} -e ${ENVIRONMENT}
