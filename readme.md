## Disclaimer

Does not currently work in ARM template projects. :/

## Overview

ARM Templates does not support relative paths. The only way to link to other files is to put them online.  
This custom tools expects relative paths and merges your ARM templates into a nested "*.linked.json" you can use for CD.

## Example

**azuredeploy.json**

    {
        "$schema": "https//schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
        "parameters": {},
        "resources": [
            {
                "templateLink": {
                    "uri": "./aresource.json"
                }
            }
        ]
    }

**aresource.json**

    {
        "type": "Microsoft.Web/connections",
        "apiVersion": "2016-06-01",
        "properties": "..."
    }

By adding the `ARMLinker` custom tool to `azuredeploy.json`, you will get another file
nested beneath `azuredeploy.json` called `azuredeploy.linked.json`:

    {
        "$schema": "https//schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
        "parameters": {},
        "resources": [
            {
                "type": "Microsoft.Web/connections",
                "apiVersion": "2016-06-01",
                "proper": "properties were probably here"
            }
        ]
    }

## Incidentally...

This works with any JSON, but the main goal is for ARM templates.  
Hence following the same convension as the supported "full online URL" feature.

## Ambitions

Specifying a path in linked JSON in order to point to nested property. For expamle a logic app definition, while ignoring parameter definitions. (In order to use ARM parameters instead)