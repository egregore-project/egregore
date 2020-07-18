# egregore

The reference implementation of the Egregore project.

![.NET Core](https://github.com/egregore-project/egregore/workflows/.NET%20Core/badge.svg?branch=master)


#### CLI commands

- `--cert|--certs|-c [--fresh]`: Generate a new self-signed root certificate, and prompt the user for trust. Automatically recycles expired root certificates, which expire after 24 hours. Passing the optional `--fresh` parameter will first remove any existing certificates, regardless if they have expired, before creating the new one.

- `--egg|-e [egg_path]`: Generates a new egg file, either at the default egg path `.egregore/default.egg`, or at a user specified `egg_path`. If the file already exists, you cannot create a new file with the same path without first removing the old path. This is for your safety, as egg files store critical data for your existing applications.

- `--keygen|-k [key_path]`: Generates a new private key pair, either at the default key path `.egregore/egregore.key`, or at a user specified `key_path`, in interactive mode, prompting the user to enter a password which is used to encrypt the new key file on disk. The user is prompted if the keyfile already exists, and is warned that generating a new key file at the same path will destroy the previously created key.

- `--server|-s [key_path]`: Starts the server in interactive mode, using the default key path, or a user specified `key_path`. The user is prompted to enter the password for the key file, in order for the server to decrypt the secret key file during server operations.

#### Running Unattended

For deployments, running unattended is necessary to avoid prompting for a password for the key file. To run unatteded, pass no arguments, or limit arguments to:

- `--nolock`: This flag prevents the server attempting to obtain an exclusive lock on the keyfile prior to booting. This should not be used except when testing multiple servers instances in development environment, as not-obtaining an exclusive lock on the file allows others to modify or remove the file while the server is running.

- `--port`: Specified the port the server listens on for incoming requests. Defaults to `5001`.

In addition, your environment must have configuration settings exposed as environment variables, for the following keys:

- `EGREGORE_EGG_FILE_PATH`: specifies the location of the egg file, or the location to create a default egg.
- `EGREGORE_KEY_FILE_PASSWORD`: in unattended deployments, you must pass the password for the encrypted key file here.

It is recommend you use your deployment server's security features to encrypt and protect these values from being read in build scripts or passed to child processes.
