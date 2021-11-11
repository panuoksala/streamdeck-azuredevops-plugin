// global websocket, used to communicate from/to Stream Deck software
// as well as some info about our plugin, as sent by Stream Deck software 
var websocket = null,
    uuid = null,
    inInfo = null,
    actionInfo = {},
    settingsModel = {};

function connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo, inActionInfo) {
    uuid = inUUID;
    actionInfo = JSON.parse(inActionInfo);
    inInfo = JSON.parse(inInfo);
    websocket = new WebSocket('ws://localhost:' + inPort);

    //initialize values
    if (actionInfo.payload.settings.settingsModel) {
        settingsModel.ProjectName = actionInfo.payload.settings.settingsModel.ProjectName;
        settingsModel.OrganizationName = actionInfo.payload.settings.settingsModel.OrganizationName;
        settingsModel.PAT = actionInfo.payload.settings.settingsModel.PAT;
        settingsModel.DefinitionId = actionInfo.payload.settings.settingsModel.DefinitionId;
        settingsModel.PipelineType = actionInfo.payload.settings.settingsModel.PipelineType;
        settingsModel.TapAction = actionInfo.payload.settings.settingsModel.TapAction;
        settingsModel.LongPressAction = actionInfo.payload.settings.settingsModel.LongPressAction;
    } else {
        settingsModel.PAT = "";
        settingsModel.OrganizationName = "";
        settingsModel.ProjectName = "";
    }

    document.getElementById('txtProjectName').value = settingsModel.ProjectName;
    document.getElementById('txtOrganizationName').value = settingsModel.OrganizationName;
    document.getElementById('txtPat').value = settingsModel.PAT;    
    document.getElementById('txtDefinitionId').value = settingsModel.DefinitionId;
    document.getElementById('pipeline_type').value = settingsModel.PipelineType;
    document.getElementById('tap_action').value = settingsModel.TapAction;
    document.getElementById('long_press_action').value = settingsModel.LongPressAction;

    websocket.onopen = function () {
        var json = { event: inRegisterEvent, uuid: inUUID };
        // register property inspector to Stream Deck
        websocket.send(JSON.stringify(json));

    };

    websocket.onmessage = function (evt) {
        // Received message from Stream Deck
        var jsonObj = JSON.parse(evt.data);
        var sdEvent = jsonObj['event'];
        switch (sdEvent) {
            case "didReceiveSettings":
                if (jsonObj.payload.settings.settingsModel.ProjectName) {
                    settingsModel.ProjectName = jsonObj.payload.settings.settingsModel.ProjectName;
                    document.getElementById('txtProjectName').value = settingsModel.ProjectName;
                }
                if (jsonObj.payload.settings.settingsModel.OrganizationName) {
                    settingsModel.OrganizationName = jsonObj.payload.settings.settingsModel.OrganizationName;
                    document.getElementById('txtOrganizationName').value = settingsModel.OrganizationName;
                }
                if (jsonObj.payload.settings.settingsModel.PAT) {
                    settingsModel.PAT = jsonObj.payload.settings.settingsModel.PAT;
                    document.getElementById('txtPat').value = settingsModel.PAT;
                }
                if (jsonObj.payload.settings.settingsModel.PipelineType) {
                    settingsModel.PipelineType = jsonObj.payload.settings.settingsModel.PipelineType;
                    document.getElementById('pipeline_type').value = settingsModel.PipelineType;
                }
                if (jsonObj.payload.settings.settingsModel.DefinitionId) {
                    settingsModel.DefinitionId = jsonObj.payload.settings.settingsModel.DefinitionId;
                    document.getElementById('txtDefinitionId').value = settingsModel.DefinitionId;
                }
                if (jsonObj.payload.settings.settingsModel.TapAction) {
                    settingsModel.TapAction = jsonObj.payload.settings.settingsModel.TapAction;
                    document.getElementById('tap_action').value = settingsModel.TapAction;
                }
                if (jsonObj.payload.settings.settingsModel.LongPressAction) {
                    settingsModel.LongPressAction = jsonObj.payload.settings.settingsModel.LongPressAction;
                    document.getElementById('long_press_action').value = settingsModel.LongPressAction;
                }
                break;
            default:
                break;
        }
    };
}

const setSettings = (value, param) => {
    if (websocket) {
        settingsModel[param] = value;
        var json = {
            "event": "setSettings",
            "context": uuid,
            "payload": {
                "settingsModel": settingsModel
            }
        };
        websocket.send(JSON.stringify(json));
    }
};

