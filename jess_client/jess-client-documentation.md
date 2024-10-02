# ENSIMS JESS Web Client Documentation

Version: 1.0.0.0
Copyright: © 2024, Energy Simulation Slotuions Ltd.

## Overview

The JESS Web Client is a command-line tool for interacting with the ENSIMS JESS (JEPlus Energy Simulation System) web service. It allows users to submit, retrieve, and manage energy simulation jobs.

The four basic operations are: check-in to the service, submit jobs, check job status and download results, and manage (cancel) jobs. The Client maintains an internal list of submitted jobs to allow off-line operations. The list is stored in the configuration file ('client.cfg' by default) along with information of the service endpoints and the current session.

## Usage

### Validate Current Session

Check the current session; show the logon dialog if the current session is invalid. If a config file is not specified, the Client will try to read from 'client.cfg' or created a new default configuration. You can edit the config file to target different service endpoints, or manage pending jobs.

```
jess_client [--cfg <config_file>] --checkin
```

### Submit a simulation job

Three job types are supported at present. These are standard E+ jobs (with 'EP_IDF'), accelerated E+ jobs ('EP_IDF_SPLIT'), and jE+ jobs ('JE_JEP'). The general form of the command is as such:

```
jess_client [--cfg <config_file>] [--checkin] --submit <EP_IDF|EP_IDF_SPLIT|JE_JEP> [--desc <title:description>] [--output <output_directory>] [--options <option1:option2:...>] <input_file1|folder1|zip1> <input_file2|folder2|zip2> ...
```

### Retrieve a Job

This operation first poll the status of the job. If it is "FINISHED", the Client will download the result files. 

```
jess_client [--cfg <config_file>] [--checkin] --retrieve <job_id|all> [--output <output_directory>] [--options <default|all|file_types|file_name>] 
```

### Cancel a Job

This operation sends a cancel command to the server. If '--cancel all' is specified, it will cancel all pending jobs.

```
jess_client [--cfg <config_file>] [--checkin] --cancel <job_id|all>
```

## Options

| Option | Description |
|--------|-------------|
| `-f, --cfg` | Specify the configuration file |
| `-x, --exitcode` | Return directly with the exit code (for debugging purpose). Default: 99 |
| `-k, --checkin` | Check in to the online service with the existing key |
| `-s, --submit` | Submit a job of the specified type: EP_IDF, EP_IDF_SPLIT or JE_JEP |
| `-d, --desc` | Specify the title and the description of the submitted job. The fields are separated by ':' |
| `-t, --options` | Specify the job submission or retrieval options. Multiple fields are separated by ':' |
| `-o, --output` | Specify the job's output folder for storing the results. This can be specified at submission or retrieving time |
| `-r, --retrieve` | Retrieve a job (with the id) or all pending jobs (with 'all'). Results are downloaded if simulation has completed, otherwise returns the job status |
| `-w, --await` | Await results - only applicable with submit and retrieve |
| `-c, --cancel` | Cancel one or more pending jobs (with the id or ids separated by ':') or all pending jobs (with 'all'). Status of the job(s) will be returned |
| `--help` | Display the help screen |
| `--version` | Display version information |

## Positional Arguments

The first positional argument (value pos. 0) specifies the file(s), zipped archives, or folder(s) to submit. Unlimited number of arguments can be supplied. If only one folder is specified, only its contents (without the folder itself) will be submitted.

## Examples

1. Check in to the service with a specific config file:

   ```
   jess_client --cfg myclient.cfg --checkin
   ```

2. Submit an EnergyPlus job:
   ```
   jess_client --output ../results --submit EP_IDF building.idf weather.epw
   ```

3. Submit a jEPlus job with Latin Hypercube Sampling:
   ```
   jess_client --output ../results --submit JE_JEP --options LHS:10 project_files.zip
   ```

4. Retrieve job results:
   ```
   jess_client --output ../results --retrieve 12345
   ```

5. Cancel a job:
   ```
   jess_client --cancel 12345
   ```

For more information or support, please refer to the ENSIMS JESS documentation or contact support.
