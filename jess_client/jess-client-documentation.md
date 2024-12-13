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

## Syntax

### Options

| Option | Description |
|--------|-------------|
| `-f, --cfg` | Specify the location of the configuration file to use |
| `-x, --exitcode` | Return directly with the exit code (for debugging purpose). Default: 99 |
| `-e, --energyplus` | Run the client in the energyplus.exe mode. Equivalent to '-s EP -o . -a in.idf in.epw' |
| `-k, --checkin` | Check in to the online service with the existing key. If check-in fails, prompt for log in; otherwise update the session key. This option can be used with other operations to keep the session alive. |
| `-s, --submit` | Submit a job of the specified type. Available job types are JEPLUS, EP, EP-SPLIT, RTRACE, RPICT, DS and AUTO |
| `-d, --desc` | Specify the title and the description of the submitted job. The fields are separated by ':' |
| `-t, --options` | Specify the job submission or retrieval options. Multiple fields are separated by ':'. The fields are dertermined by the job types; see below for furhter details |
| `-g, --args` | Only used by Radiance jobs for specifying rtrace/rpict program arguments in a file, e.g. with @args_file |
| `-o, --output` | Specify the job's output folder for storing the results. This can be specified at submission or retrieving time |
| `-a, --await` | Works with --submit and --retrieve only. Awaits the results to become available and retrieve |
| `-r, --retrieve` | Retrieve a job (with the id) or all pending jobs (with 'all'). Results are downloaded if simulation has completed, otherwise returns the job status |
| `-w, --watch` | Watch a folder |
| `-c, --cancel` | Cancel one or more pending jobs (with the id or ids separated by ':') or all pending jobs (with 'all'). Status of the job(s) will be returned |
| `--help` | Display the help screen |
| `--version` | Display version information |


More about `--options <field1>[:<field2>[:<field3>]]`:

| Job type | Field1 | Field2 | Field3 | example |
|---|---|---|---|---|
| `submit` <td colspan=4> |
| EP / EP-SPLIT | Model file name | Weather file name | - | `--options my.idf:my.epw` |
| JEPLUS | Subset type: ALL, LHS, LIST_FILE | Subset param: sample size or list file name  | - | `--options LHS:10`, `--options LIST_FILE:joblist.txt` | 
| RTRACE | Model file name | Input file(s) | Output file ext. | `--options mymodel.oct:*.in:out` |
| RPICT | Model file name | Input file(s) | Output file ext. | `--options mymodel.oct:*.vf:hdr` |
| DAYSIM | Header file name | Input file(s) | - | `--options my.hea:*.in` |
| `retrieve` <td colspan=4> (not available with `--retrieve all`) |
| <td colspan=3> Comma-separated list of extentions of the Files to download | `--options eso,mtr,htm` |


### Arguments

The first positional argument (value pos. 0) specifies the file(s), zipped archives, or folder(s) to submit. Unlimited number of arguments can be supplied. If only one folder is specified, only its contents (without the folder itself) will be submitted.

### The configuration file

When --cfg is not specified, the program attempts to locate "client.cfg" in the following three places and use the first one found to be writeable: (1) C:\Users\{current_user}\AppData\Local\jess_client\, (2) where jess_client.exe is located, and (3) the current folder (.) where the program is run. Here is an example of the content of a config file used by the client program. 

```
{
  "Comment": "Client configuration saved at 2024-12-01_22.19.47",
  "SessionKey": "eyJraWQiOiIxNTE4MTE0OTI2NTQ5IiwiYWxnIjoiUlMyNTYifQ....",
  "ClientId": "JessWebClient",
  "PendingJobs": {
    "12572": {
      "Id": 12572,
      "Opt": null,
      "TargetFolder": "C:\\dev\\vs2017\\jess_client\\bin\\Debug\\example_e+v22.1"
    }
  },
  "WatchedFolders": [],
  "ServerUrl": "https://api.ensims.com",
  "AuthEndPoint": "/users/api",
  "JessEndPoint": "/jess_web/api"
}
```

### Exit code

The program generates a number of console outputs for information, warning and errors. The same outputs are stored in jess_client.log. Depending on the operations being successful or not, the program may terminate with different exit codes (%errorlevel%):

| Exit code | Description |
|--------|-------------|
| 0 | Normal termination (success) |
| 1 | Unauthorized operation. Need to use --checkin to update the session key |
| 2 | Server-side error. It could be caused by a number of things. Contact support if you are possitive that your submision is correct |
| 3 | Client-side error. For example, the file to submit is not found. |
| 4 | Unknown error, showing the operation has failed due to an unspecified reason. Contact support with the logs |
| -99 | JESS service unavailable. This could be caused by the service bing offline, or you do not have access to it due to network restrictions |


## More examples

1. Check in to the service with a specific config file:

   ```
   jess_client --cfg myclient.cfg --checkin
   ```

2. Submit an EnergyPlus job with a pair of idf and epw files, and output to be saved in a folder named 'results' next to the current folder:

   ```
   jess_client --checkin --output ../results --submit EP example_e+v22.2/5ZoneAirCooled_v22.2.idf example_e+v22.2/in.epw
   ```

3. Submit an EnergyPlus job with all the files in a folder, and output to be saved in a folder named 'results' under the the current folder. The model and the epw files will be determined by the server:

   ```
   jess_client --checkin --output results --submit EP example_e+v22.2
   ```

4. Submit an accelerated EnergyPlus job with all the files in a folder, and output to be saved in a folder named 'results' under the the current folder. The model and the epw files are specified using --options; and the job information with --desc :

   ```
   jess_client --submit EP-SPLIT --desc "my job:the model of abcde" --options 5ZoneAirCooled_v23.1.idf:in.epw --output results example_e+v23.1
   ```

5. If multipe EnergyPlus models/weather files are present in the submission, they will be assembled automatically into a batch:

   ```
   jess_client --checkin --submit EP --desc "my batch job:benchmark models" --output batch_results example_e+_multiple_idf
   ```

6. Submit a jEPlus job with Latin Hypercube Sampling. The project files are all located in the given folder.

   ```
   jess_client --submit JEPLUS --desc "jeplus test:project with test scripts" --options LHS:10 --output jeplus_results example_jeplus_v2.1_scripts_E+v8.9
   ```

7. Submit a Radiance Rtrace job. The submission need to contain a .oct file, one or more sensor definition file(s) (use wildcard char *), and the *extenion* of the output readings. The Rtrace commandline arguments can be passed in using `--args @args_file`, where the arguements string is saved in a text file.

   ```
   jess_client --checkin --submit RTRACE --desc "my rtrace job:some descritpion" --output rtrace_results --options dbmodel.oct:trace.in:out --args @args.txt example_Rtrace
   ```

   Multiple sensor files will be simulated in parallel.
   ```
   jess_client --checkin --submit RTRACE --desc "my rtrace2 job:some descritpion" --output rtrace_results --options dbmodel.oct:*.in:out --args @args.txt example_Rtrace2
   ```

8. Submit a Radiance Rpict job. The submission need to contain a .oct file, one or more view definition file(s) (use wildcard char *), and the *extenion* of the output hdr images. The Rpict commandline arguments can be passed in using `--args @args_file`, where args_file stands for the name of a text file encapsulating the actual arguements string.

   ```
   jess_client --checkin --submit RPICT --desc "my rpict job:some descritpion" --output rpict_results --options scene.oct:viewpoint.txt:hdr --args @args.txt example_Rpict1
   ```

   Multiple view definition files will be simulated in parallel.
   ```
   jess_client --checkin --submit RPICT --desc "my rpict2 job:some descritpion" --output rpict_results --options dbmodel.oct:*.vf:hdr --args @args.txt example_Rpict2
   ```

9. Submit a DaySim job. The submission need to contain a DaySim header file and one or more sensor file(s) (use wildcard char *). 

   ```
   jess_client --checkin --submit DAYSIM --desc "my daysim job:some descritpion" --output daysim_results --options Daysim.txt:trace.in example_ds1
   ```

   Multiple sensor files will be simulated in parallel.

   ```
   jess_client --checkin --submit DAYSIM --desc "my daysim2 job:some descritpion" --output daysim_results --options Daysim.txt:*.in example_ds2
   ```

10. Retrieve certain result files of a job and save them to a specified location (other than that specified at submission):

   ```
   jess_client --retrieve 622966 --options eso,mtr,htm --output 622966_output
   ```

11. Await all pending jobs to finish and retrieve the results:

   ```
   jess_client --retrieve all --await
   ```

12. Cancel a job:

   ```
   jess_client --cancel 12345
   ```

For more information or support, please refer to the ENSIMS JESS documentation or contact support at jess@ensims.com.
