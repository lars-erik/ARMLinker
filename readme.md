## Overview

ARM Templates does not support local and relative paths. The only way to link to other files is to put them online.  
This tool expects local and/or relative paths and merges your ARM templates into a one template.  
A fuller narrative with example usage can be read on [my blog here](http://blog.aabech.no/archive/armlinker-100-released/).

## Install

    install-module ARMLinker

## PowerShell Usage

    import-module ARMLinker
    Convert-TemplateLinks -InputPath <path to main template> [-OutputPath <path to output file>]

If no output path is specified, the merged JSON is output from the cmdlet.

## Example input/output

*Example shows optimal usage with VS custom tool, but for now only way is to use the PS-module*

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

If the JSON you want to link is at some deeper level in the linked JSON, you can specify a "jsonPath" as well:

**azuredeploy.json**

    {
        "$schema": "https//schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
        "parameters": {},
        "resources": [
            {
                "templateLink": {
                    "uri": "./aresource.json",
                    "jsonPath": "special.path"
                }
            }
        ]
    }

**aresource.json**

    {
        "special": {
            "path": {
                "type": "Microsoft.Web/connections",
                "apiVersion": "2016-06-01",
                "properties": "..."
            }
        }
    }


## Incidentally...

This works with any JSON, but the main goal is for ARM templates.  
Hence following the same convension as the supported "full online URL" feature.
