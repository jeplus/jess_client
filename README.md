# ENSIMS JESS Web Client (jess_client.exe)

Version: 1.0.0.0
Copyright: © 2024, Energy Simulation Solutions Ltd.

## Overview

The JESS Web Client is a command-line tool for interacting with the ENSIMS JESS (JEPlus Energy Simulation System) web service. It allows users to submit, retrieve, and manage energy simulation jobs.

The four basic operations are: check-in to the service, submit jobs, check job status and download results, and manage (cancel) jobs. The Client maintains an internal list of submitted jobs to allow off-line operations. The list is stored in the configuration file ('client.cfg' by default) along with information of the service endpoints and the current session.

## Usage

### Validate/extend Current Session

Check-in the current session to update the session key; show the login dialog if the current session is invalid. If a config file is not specified, the Client will try to locate the 'client.cfg' or to create a new default configuration. You can edit the config file to target different service endpoints, or manage pending jobs.

```
jess_client [--cfg <config_file>] --checkin
```

### Submit a simulation job

Three job types are supported at present. These are standard E+ jobs (with 'EP'), accelerated E+ jobs ('EP_SPLIT'), jE+ jobs ('JEPLUS'), and so on. The general form of the command is as such:

```
jess_client [--cfg <config_file>] [--checkin] --submit JEPLUS|EP|EP-SPLIT|RTRACE|RPICT|DAYSIM|AUTO [--desc <title>[:<description>]] [--output <output_directory>] [--options <option1>:<option2>:...>] [--args @<argsfile>] [--await] <input_file1>|<folder1>|<zip1> [<input_file2>|<folder2>|<zip2>] [...]
```

### Submit a job in EnergyPlus mode

This is a short-hand to submit a "standard" EnergyPlus job, i.e. an EP job with in.idf and in.epw in the current folder, and the simulation result are automatically retrieved and saved to the current folder.

```
jess_client [--checkin] --energyplus
```

It is equivalent to:

```
jess_client [--checkin] --submit EP --output . --await in.idf in.epw
```


### Retrieve a Job result

This operation first poll the status of the job. If it is "FINISHED", the Client will download the result files. 

```
jess_client [--cfg <config_file>] [--checkin] --retrieve <job_id>|all [--output <output_directory>] [--options <file_ext1>,<file_ext2>,...] [--await]
```

### Cancel a Job

This operation sends a cancel command to the server. If '--cancel all' is specified, it will cancel all pending jobs.

```
jess_client [--cfg <config_file>] [--checkin] --cancel <job_id>|all
```

### Watch a folder

This operation starts the client to watch the specified folder(s), submit jobs and automatically retrieve results when the folder contents has changed or a marker file is present.

```
jess_client [--cfg <config_file>] [--checkin] --watch <folder1> [<folder2>] [...]
```
