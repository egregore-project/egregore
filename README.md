# egregore

The reference implementation of the Egregore project.

![.NET Core](https://github.com/egregore-project/egregore/workflows/.NET%20Core/badge.svg?branch=master)

#### Running on Cloud Providers

Pass no arguments, or limit arguments to `--nolock` and `--port`, to run in unattended server mode. 

Your application must have configuration settings available as environment variables, for the following keys:

- `EGREGORE_EGG_FILE_PATH`: specifies the location of the egg file, or the location to create a default egg.
- `EGREGORE_KEY_FILE_PASSWORD`: in unattended deployments, you must pass the password for the encrypted key file here.
