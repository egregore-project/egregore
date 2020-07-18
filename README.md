# egregore

The reference implementation of the Egregore project.

![.NET Core](https://github.com/egregore-project/egregore/workflows/.NET%20Core/badge.svg?branch=master)

#### Deploying to Azure

Pass no arguments to run in uninteractive server mode. Your application must have configuration settings available as environment variables, for the
following keys:

- `EGREGORE_EGG_FILE_PATH`: specifies the location of the egg file, or the location to create a default egg.
   _On Linux, this path must refer to a volume with -nobrl enabled, as SQLite will not function correctly without it._

- `EGREGORE_KEY_FILE_PASSWORD`: in unattended deployments, you must pass the password for the encrypted key file here.
